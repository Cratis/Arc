// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Cratis.Arc.Chronicle.CodeAnalysis;

/// <summary>
/// Analyzer that warns when a reactor handler invoking <c>ICommandPipeline.Execute</c> does not say what should
/// happen to that call during a replay — neither <c>[OnceOnly]</c> nor a <c>[Replay]</c> handler for the same
/// event type.
/// </summary>
/// <remarks>
/// Chronicle dispatches to a handler whose first parameter is an event type using
/// <c>BindingFlags.Instance | Public | NonPublic</c> — a private method is a real dispatch target whenever no
/// public method claims the same event type. This analyzer follows a call from any such candidate handler,
/// through private helpers on the same reactor, to the <c>Execute</c> call it reaches, rather than looking only
/// at the immediate method containing the call.
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ReactorCommandPipelineExecuteOnceOnlyAnalyzer : DiagnosticAnalyzer
{
    const string ExecuteMethodName = "Execute";
    const string ReactorInterfaceName = "IReactor";
    const string ReactorsNamespace = "Cratis.Chronicle.Reactors";
    const string OnceOnlyAttributeName = "OnceOnlyAttribute";
    const string ReplayAttributeName = "ReplayAttribute";
    const string CommandPipelineInterfaceName = "ICommandPipeline";
    const string CommandsNamespace = "Cratis.Arc.Commands";
    const string EventTypeAttributeName = "EventTypeAttribute";
    const string EventsNamespace = "Cratis.Chronicle.Events";

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [DiagnosticDescriptors.ARCCHR0006_ReactorCommandPipelineExecuteNeedsReplayDecision];

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolStartAction(AnalyzeReactor, SymbolKind.NamedType);
    }

    static void AnalyzeReactor(SymbolStartAnalysisContext context)
    {
        var typeSymbol = (INamedTypeSymbol)context.Symbol;

        if (typeSymbol.TypeKind != TypeKind.Class || !IsReactor(typeSymbol) || HasOnceOnlyAttribute(typeSymbol))
        {
            return;
        }

        var state = new ReactorState();

        context.RegisterOperationAction(operationContext => RecordInvocation(operationContext, typeSymbol, state), OperationKind.Invocation);
        context.RegisterSymbolEndAction(symbolEndContext => ReportUndecidedHandlers(symbolEndContext, typeSymbol, state));
    }

    static void RecordInvocation(OperationAnalysisContext context, INamedTypeSymbol reactorType, ReactorState state)
    {
        var enclosingMethod = FindEnclosingMethod(context.ContainingSymbol);
        if (enclosingMethod is null)
        {
            return;
        }

        var invocation = (IInvocationOperation)context.Operation;
        var target = invocation.TargetMethod;

        if (target.Name == ExecuteMethodName && IsCommandPipeline(target.ContainingType))
        {
            state.Executions.GetOrAdd(enclosingMethod, _ => invocation.Syntax.GetLocation());
        }
        else if (SymbolEqualityComparer.Default.Equals(target.OriginalDefinition.ContainingType, reactorType))
        {
            state.Calls.GetOrAdd(enclosingMethod, _ => []).Add(target.OriginalDefinition);
        }
    }

    static void ReportUndecidedHandlers(SymbolAnalysisContext context, INamedTypeSymbol reactorType, ReactorState state)
    {
        var candidates = reactorType.GetMembers().OfType<IMethodSymbol>().Where(IsHandlerCandidate).ToArray();

        foreach (var execution in state.Executions)
        {
            var handlers = candidates
                .Where(handler => IsDispatched(handler, candidates) && !IsExcused(handler, candidates) && Reaches(handler, execution.Key, state))
                .OrderBy(handler => handler.Name, StringComparer.Ordinal)
                .ToArray();

            if (handlers.Length == 0)
            {
                continue;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.ARCCHR0006_ReactorCommandPipelineExecuteNeedsReplayDecision,
                execution.Value,
                FormatNames(handlers)));
        }
    }

    /// <summary>
    /// Climbs from the symbol containing an operation to the nearest enclosing ordinary method, passing through
    /// any lambda or local function the operation sits inside so a call made from either is still attributed to
    /// the handler that declares it.
    /// </summary>
    /// <param name="symbol">The symbol to climb from.</param>
    /// <returns>The nearest <see cref="IMethodSymbol"/> with <see cref="MethodKind.Ordinary"/>, or <see langword="null"/> when none is found.</returns>
    static IMethodSymbol? FindEnclosingMethod(ISymbol? symbol)
    {
        for (var current = symbol; current is not null; current = current.ContainingSymbol)
        {
            if (current is IMethodSymbol { MethodKind: MethodKind.Ordinary } method)
            {
                return method;
            }
        }

        return null;
    }

    /// <summary>
    /// Determines whether a method has the shape of an event handler — dispatch is driven entirely by the
    /// first parameter carrying <c>[EventType]</c>, never by return type: an unrecognized return type on such a
    /// method throws <c>InvalidReactorHandlerReturnType</c> at registration rather than being silently skipped.
    /// </summary>
    /// <param name="method">The method to check.</param>
    /// <returns>True if the method is a handler candidate, false otherwise.</returns>
    static bool IsHandlerCandidate(IMethodSymbol method) =>
        method.MethodKind == MethodKind.Ordinary &&
        !method.IsStatic &&
        method.Parameters.Length >= 1 &&
        HasEventTypeAttribute(method.Parameters[0].Type);

    /// <summary>
    /// Determines whether Chronicle would actually dispatch to this handler for its event type — a public
    /// handler always is; a non-public one only is when no public candidate claims the same event type.
    /// </summary>
    /// <param name="handler">The candidate handler.</param>
    /// <param name="candidates">Every handler candidate on the reactor.</param>
    /// <returns>True if the handler is a dispatch target, false otherwise.</returns>
    static bool IsDispatched(IMethodSymbol handler, IReadOnlyCollection<IMethodSymbol> candidates) =>
        handler.DeclaredAccessibility == Accessibility.Public ||
        !candidates.Any(other =>
            !SymbolEqualityComparer.Default.Equals(other, handler) &&
            other.DeclaredAccessibility == Accessibility.Public &&
            SymbolEqualityComparer.Default.Equals(other.Parameters[0].Type, handler.Parameters[0].Type));

    /// <summary>
    /// Determines whether a replay decision has already been made for this handler — either on the handler
    /// itself, or by a sibling <c>[Replay]</c> handler declared for the same event type. Whether that replay
    /// handler is genuinely a no-op is not this analyzer's business; declaring one is itself the statement that
    /// replay was considered.
    /// </summary>
    /// <param name="handler">The candidate handler.</param>
    /// <param name="candidates">Every handler candidate on the reactor.</param>
    /// <returns>True if the handler is excused from needing a replay decision, false otherwise.</returns>
    static bool IsExcused(IMethodSymbol handler, IReadOnlyCollection<IMethodSymbol> candidates) =>
        HasOnceOnlyAttribute(handler) ||
        HasReplayAttribute(handler) ||
        candidates.Any(other =>
            !SymbolEqualityComparer.Default.Equals(other, handler) &&
            SymbolEqualityComparer.Default.Equals(other.Parameters[0].Type, handler.Parameters[0].Type) &&
            HasReplayAttribute(other));

    /// <summary>
    /// Depth-first walks the intra-type call graph from a handler, returning whether it reaches the given
    /// target method — directly, or through any number of private helpers on the same reactor.
    /// </summary>
    /// <param name="handler">The method to walk from.</param>
    /// <param name="target">The method to reach.</param>
    /// <param name="state">The per-reactor recorded executions and calls.</param>
    /// <returns>True if the target is reached, false otherwise.</returns>
    static bool Reaches(IMethodSymbol handler, IMethodSymbol target, ReactorState state) =>
        ReachesCore(handler, target, state, new HashSet<IMethodSymbol>(SymbolEqualityComparer.Default));

    static bool ReachesCore(IMethodSymbol current, IMethodSymbol target, ReactorState state, HashSet<IMethodSymbol> visited)
    {
        if (SymbolEqualityComparer.Default.Equals(current, target))
        {
            return true;
        }

        if (!visited.Add(current) || !state.Calls.TryGetValue(current, out var callees))
        {
            return false;
        }

        return callees.Any(callee => ReachesCore(callee, target, state, visited));
    }

    /// <summary>
    /// Renders handler names for the diagnostic message: a single name on its own, two joined with "and", and
    /// three or more as a comma-separated list with "and" before the last.
    /// </summary>
    /// <param name="handlers">The handlers to render, already sorted.</param>
    /// <returns>The rendered names.</returns>
    static string FormatNames(IReadOnlyList<IMethodSymbol> handlers)
    {
        var names = handlers.Select(handler => $"'{handler.Name}'").ToArray();

        return names.Length switch
        {
            1 => names[0],
            2 => $"{names[0]} and {names[1]}",
            _ => $"{string.Join(", ", names.Take(names.Length - 1))} and {names[names.Length - 1]}"
        };
    }

    static bool IsReactor(INamedTypeSymbol typeSymbol) =>
        typeSymbol.AllInterfaces.Any(@interface =>
            @interface.Name == ReactorInterfaceName &&
            @interface.ContainingNamespace?.ToDisplayString() == ReactorsNamespace);

    static bool IsCommandPipeline(ITypeSymbol? typeSymbol)
    {
        if (typeSymbol is null)
        {
            return false;
        }

        if (IsCommandPipelineInterface(typeSymbol))
        {
            return true;
        }

        return typeSymbol.AllInterfaces.Any(IsCommandPipelineInterface);
    }

    static bool IsCommandPipelineInterface(ITypeSymbol typeSymbol) =>
        typeSymbol.Name == CommandPipelineInterfaceName &&
        typeSymbol.ContainingNamespace?.ToDisplayString() == CommandsNamespace;

    static bool HasOnceOnlyAttribute(ISymbol symbol) =>
        symbol.GetAttributes().Any(attribute =>
            attribute.AttributeClass?.Name == OnceOnlyAttributeName &&
            attribute.AttributeClass?.ContainingNamespace?.ToDisplayString() == ReactorsNamespace);

    static bool HasReplayAttribute(ISymbol symbol) =>
        symbol.GetAttributes().Any(attribute =>
            attribute.AttributeClass?.Name == ReplayAttributeName &&
            attribute.AttributeClass?.ContainingNamespace?.ToDisplayString() == ReactorsNamespace);

    static bool HasEventTypeAttribute(ITypeSymbol type) =>
        type.GetAttributes().Any(attribute =>
            attribute.AttributeClass?.Name == EventTypeAttributeName &&
            attribute.AttributeClass?.ContainingNamespace?.ToDisplayString() == EventsNamespace);

    /// <summary>
    /// Per-reactor analysis state accumulated across the concurrently-executed operation actions registered for
    /// one <see cref="SymbolStartAnalysisContext"/>. Both dictionaries must be thread-safe.
    /// </summary>
    sealed class ReactorState
    {
        /// <summary>
        /// Gets the first <c>ICommandPipeline.Execute</c> call site seen per enclosing method.
        /// </summary>
        public ConcurrentDictionary<IMethodSymbol, Location> Executions { get; } = new(SymbolEqualityComparer.Default);

        /// <summary>
        /// Gets, per method, the set of other methods on the same reactor that it calls.
        /// </summary>
        public ConcurrentDictionary<IMethodSymbol, ConcurrentBag<IMethodSymbol>> Calls { get; } = new(SymbolEqualityComparer.Default);
    }
}
