// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Arc.Commands;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cratis.Arc.Chronicle.CodeAnalysis;

/// <summary>
/// Advises on recognizable unguarded Chronicle reads and immediate appends in event-producing commands.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class CommandDecisionReadAnalyzer : DiagnosticAnalyzer
{
    const string CommandAttribute = "Cratis.Arc.Commands.ModelBound.CommandAttribute";
    const string ChronicleReadModels = "Cratis.Chronicle.ReadModels.IReadModels";
    const string DecisionReads = "Cratis.Chronicle.ReadModels.IDecisionReads";
    const string EventLog = "Cratis.Chronicle.EventSequences.IEventLog";
    const string HandlerAdvice = "Enable ReadProtection(On) and take the read model as a parameter, or mark the intentional advisory read [Unprotected(\"why\")]";
    const string ValidatorAdvice = "Enable ReadProtection(On) and take the read model in the CommandValidator constructor for state-dependent rules; under Off this read is only a hint";

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
    [
        DiagnosticDescriptors.ARCCHR0011_UnprotectedDecisionRead,
        DiagnosticDescriptors.ARCCHR0012_ImmediateAppendAfterDecisionRead
    ];

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(start =>
        {
            // Projection and reducer targets are computed once per compilation, and only when a candidate parameter needs them.
            var backedTypes = new Lazy<ImmutableHashSet<INamedTypeSymbol>>(
                () => ProjectionAndReducerTargets(start.Compilation),
                LazyThreadSafetyMode.ExecutionAndPublication);
            start.RegisterSyntaxNodeAction(_ => AnalyzeParameter(_, backedTypes), SyntaxKind.Parameter);
            start.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
        });
    }

    static void AnalyzeParameter(SyntaxNodeAnalysisContext context, Lazy<ImmutableHashSet<INamedTypeSymbol>> backedTypes)
    {
        var syntax = (ParameterSyntax)context.Node;

        // Lambda and local function parameters (for example the x in RuleFor(x => ...)) inject nothing.
        if (syntax.Parent?.Parent is not BaseMethodDeclarationSyntax and not TypeDeclarationSyntax)
        {
            return;
        }

        if (context.SemanticModel.GetDeclaredSymbol(syntax, context.CancellationToken) is not IParameterSymbol { ContainingSymbol: IMethodSymbol method } parameter)
        {
            return;
        }

        if (!InDecision(method, out var command))
        {
            return;
        }

        // A validator's reads are injected through its constructor; other validator methods are not resolved from DI.
        var inValidator = !SymbolEqualityComparer.Default.Equals(command, method.ContainingType);
        if (IsNotAdvisedParameter(command, method, parameter, inValidator) || !IsChronicleBacked(parameter.Type, backedTypes))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.ARCCHR0011_UnprotectedDecisionRead,
            syntax.GetLocation(),
            command.Name,
            parameter.Type.Name,
            inValidator ? ValidatorAdvice : HandlerAdvice));
    }

    static bool IsNotAdvisedParameter(INamedTypeSymbol command, IMethodSymbol method, IParameterSymbol parameter, bool inValidator) =>
        (inValidator && method.MethodKind != MethodKind.Constructor) ||
        ReadProtectionSymbols.Mode(command) != 0 ||
        HasUnprotected(command) || HasUnprotected(method) || HasUnprotected(parameter) || HasUnprotected(parameter.Type) ||
        IsDecision(parameter.Type);

    static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        var syntax = (InvocationExpressionSyntax)context.Node;
        if (context.SemanticModel.GetEnclosingSymbol(syntax.SpanStart, context.CancellationToken) is not IMethodSymbol enclosing ||
            context.SemanticModel.GetSymbolInfo(syntax, context.CancellationToken).Symbol is not IMethodSymbol called)
        {
            return;
        }

        var immediateAppend = IsImmediateAppend(syntax, called, context.SemanticModel, context.CancellationToken);
        if (!InDecision(enclosing, out var command, immediateAppend) || HasUnprotected(command))
        {
            return;
        }

        ReportUnprotectedRead(context, called, enclosing, command);

        if (immediateAppend)
        {
            ReportImmediateAppend(context, syntax, enclosing, command);
        }
    }

    static void ReportUnprotectedRead(SyntaxNodeAnalysisContext context, IMethodSymbol called, IMethodSymbol enclosing, INamedTypeSymbol command)
    {
        if (ReadProtectionSymbols.Mode(command) != 0 || !IsReadModelsLookup(called))
        {
            return;
        }

        if (!HasUnprotected(enclosing) && IsCommand(command))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.ARCCHR0011_UnprotectedDecisionRead,
                context.Node.GetLocation(),
                command.Name,
                called.TypeArguments.FirstOrDefault()?.Name ?? "read model",
                SymbolEqualityComparer.Default.Equals(command, enclosing.ContainingType) ? HandlerAdvice : ValidatorAdvice));
        }
    }

    static bool IsReadModelsLookup(IMethodSymbol called) =>
        called.Name == "GetInstanceById" && Implements(ReceiverOf(called), ChronicleReadModels);

    static void ReportImmediateAppend(SyntaxNodeAnalysisContext context, InvocationExpressionSyntax syntax, IMethodSymbol enclosing, INamedTypeSymbol command)
    {
        var usesDecision = UsesDecision(context, enclosing, command);
        var mode = ReadProtectionSymbols.Mode(command);
        if (mode == 2 || usesDecision || CommandAttributeSymbols.ResolveCommand(command).Mode == 1)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.ARCCHR0012_ImmediateAppendAfterDecisionRead,
                syntax.GetLocation(),
                mode == 2 ? DiagnosticSeverity.Warning : DiagnosticSeverity.Info,
                null,
                null,
                command.Name));
        }
    }

    static bool UsesDecision(SyntaxNodeAnalysisContext context, IMethodSymbol enclosing, INamedTypeSymbol command) =>
        HasDecisionParameter(command.GetMembers().OfType<IMethodSymbol>().Where(IsHandlerOrProvider)) ||
        HasDecisionParameter(enclosing.ContainingType.GetMembers().OfType<IMethodSymbol>()) ||
        InvokesDecisionReads(context, enclosing);

    static bool HasDecisionParameter(IEnumerable<IMethodSymbol> methods) =>
        methods.Any(_ => _.Parameters.Any(parameter => IsDecision(parameter.Type)));

    static bool InvokesDecisionReads(SyntaxNodeAnalysisContext context, IMethodSymbol enclosing)
    {
        var syntax = enclosing.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax(context.CancellationToken);
        return syntax?.DescendantNodes().OfType<InvocationExpressionSyntax>().Any(_ => IsDecisionReadInvocation(context, _)) == true;
    }

    static bool IsDecisionReadInvocation(SyntaxNodeAnalysisContext context, InvocationExpressionSyntax syntax) =>
        context.SemanticModel.GetSymbolInfo(syntax, context.CancellationToken).Symbol is IMethodSymbol invoked &&
        Implements(invoked.ContainingType, DecisionReads);

    static ITypeSymbol ReceiverOf(IMethodSymbol method)
    {
        // An application wrapper such as GetInstanceById(this IReadModels, EventSourceId) is an extension method, so its containing
        // type is the static class and the read goes through the receiver.
        if (method.ReducedFrom is not null)
        {
            return method.ReceiverType ?? method.ContainingType;
        }
        return method.IsExtensionMethod && method.Parameters.Length > 0 ? method.Parameters[0].Type : method.ContainingType;
    }

    static bool InDecision(IMethodSymbol method, out INamedTypeSymbol command, bool allowImmediateAppend = false)
    {
        command = method.ContainingType;
        if (IsCommandScope(command, allowImmediateAppend))
        {
            return IsHandlerOrProvider(method);
        }

        for (var current = command.BaseType; current is not null; current = current.BaseType)
        {
            if (ValidatedCommand(current) is { } candidate)
            {
                command = candidate;
                return true;
            }
        }
        return false;
    }

    static bool IsCommandScope(INamedTypeSymbol command, bool allowImmediateAppend) =>
        IsCommand(command) || IsImmediateAppendScope(command, allowImmediateAppend);

    static bool IsImmediateAppendScope(INamedTypeSymbol command, bool allowImmediateAppend) =>
        allowImmediateAppend && HasCommandAttribute(command);

    static bool IsHandlerOrProvider(IMethodSymbol method) => method.Name == "Handle" || method.Name == "Provide";

    static INamedTypeSymbol? ValidatedCommand(INamedTypeSymbol type)
    {
        if (!IsCommandValidator(type))
        {
            return null;
        }

        return type.TypeArguments[0] is INamedTypeSymbol candidate && IsCommand(candidate) ? candidate : null;
    }

    static bool IsCommandValidator(INamedTypeSymbol type) =>
        type.IsGenericType && type.Name == "CommandValidator" &&
        type.ContainingNamespace.ToDisplayString() == "Cratis.Arc.Commands";

    static bool HasCommandAttribute(INamedTypeSymbol type) => type.GetAttributes().Any(_ => _.AttributeClass?.ToDisplayString() == CommandAttribute);

    static bool IsCommand(INamedTypeSymbol type) => HasCommandAttribute(type) &&
        type.GetMembers("Handle").OfType<IMethodSymbol>().Any(IsEventHandler);

    static bool IsEventHandler(IMethodSymbol method) =>
        ProducesEvents(method.ReturnType) || method.Parameters.Any(_ => IsAggregate(_.Type));

    static bool HasUnprotected(ISymbol symbol) => CommandAttributeSymbols.HasUnprotected(symbol);

    static bool IsDecision(ITypeSymbol type) =>
        type.OriginalDefinition.ToDisplayString() == "Cratis.Chronicle.ReadModels.DecisionRead<T>" || Implements(type, DecisionReads);

    static bool IsAggregate(ITypeSymbol type)
    {
        for (var current = type as INamedTypeSymbol; current is not null; current = current.BaseType)
        {
            if (current.ToDisplayString() == "Cratis.Arc.Chronicle.Aggregates.AggregateRoot")
            {
                return true;
            }
        }
        return false;
    }

    static bool ProducesEvents(ITypeSymbol type)
    {
        // The command pipeline awaits Task/ValueTask, then unwraps IOneOf branches (Result, OneOf) and tuple elements, so mirror
        // that nesting: Task<Result<EventsWithConcurrencyScopes, ValidationResult>> still produces events.
        if (type is INamedTypeSymbol { IsGenericType: true } named)
        {
            if (IsAwaitableOrNullable(named))
            {
                return ProducesEvents(named.TypeArguments[0]);
            }

            if (IsEventUnion(named))
            {
                return named.TypeArguments.Any(ProducesEvents);
            }
        }
        if (type is INamedTypeSymbol { IsTupleType: true } tuple)
        {
            return tuple.TupleElements.Any(_ => ProducesEvents(_.Type));
        }
        return IsEventValue(type);
    }

    static bool IsAwaitableOrNullable(INamedTypeSymbol type) =>
        type.Name == "Task" || type.Name == "ValueTask" || type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T;

    static bool IsEventUnion(INamedTypeSymbol type) =>
        type.Name == "Result" || type.Name == "OneOf" || type.AllInterfaces.Any(IsOneOfInterface);

    static bool IsOneOfInterface(INamedTypeSymbol type) =>
        type.Name == "IOneOf" && type.ContainingNamespace.ToDisplayString() == "OneOf";

    static bool IsEventValue(ITypeSymbol type)
    {
        if (type is IArrayTypeSymbol array)
        {
            // An object[] is an IEnumerable<object>, which the pipeline accepts as a set of events.
            return IsObjectOrEventValue(array.ElementType);
        }

        if (IsDeclaredEventValue(type))
        {
            return true;
        }
        if (type is INamedTypeSymbol { IsGenericType: true } named && IsEnumerableShape(named))
        {
            return named.TypeArguments.Any(IsObjectOrEventValue);
        }
        return false;
    }

    static bool IsObjectOrEventValue(ITypeSymbol type) => type.SpecialType == SpecialType.System_Object || IsEventValue(type);

    static bool IsDeclaredEventValue(ITypeSymbol type) =>
        type.ToDisplayString() == "Cratis.Chronicle.EventSequences.EventsWithConcurrencyScopes" || type.ToDisplayString() == "Cratis.Chronicle.EventSequences.EventForEventSourceId" ||
        type.GetAttributes().Any(_ => _.AttributeClass?.ToDisplayString() == "Cratis.Chronicle.Events.EventTypeAttribute");

    static bool IsEnumerableShape(INamedTypeSymbol type) =>
        type.AllInterfaces.Concat([type]).Any(_ => _.OriginalDefinition.ToDisplayString() == "System.Collections.Generic.IEnumerable<T>");

    static bool IsChronicleBacked(ITypeSymbol type, Lazy<ImmutableHashSet<INamedTypeSymbol>> backedTypes)
    {
        if (type is not INamedTypeSymbol named || type.SpecialType != SpecialType.None)
        {
            return false;
        }
        if (named.GetAttributes().Any(IsModelBoundProjection))
        {
            return true;
        }
        return HasModelBoundMember(named) || backedTypes.Value.Contains(named);
    }

    static bool IsModelBoundProjection(AttributeData attribute) =>
        attribute.AttributeClass?.ContainingNamespace.ToDisplayString() == "Cratis.Chronicle.Projections.ModelBound" &&
        IsProjectionName(attribute.AttributeClass.Name);

    static bool IsProjectionName(string name) =>
        name.StartsWith("From", StringComparison.Ordinal) || name.StartsWith("RemovedWith", StringComparison.Ordinal);

    static bool HasModelBoundMember(INamedTypeSymbol type)
    {
        // Model-bound projections are also declared on properties and record parameters (ChildrenFrom, Join, SetFrom, Count and so on).
        return type.GetMembers().OfType<IPropertySymbol>().Any(_ => HasModelBoundAttribute(_.GetAttributes())) ||
            type.InstanceConstructors.Any(_ => _.Parameters.Any(p => HasModelBoundAttribute(p.GetAttributes())));
    }

    static bool HasModelBoundAttribute(ImmutableArray<AttributeData> attributes) =>
        attributes.Any(_ => _.AttributeClass is { } attribute &&
            attribute.ContainingNamespace.ToDisplayString() == "Cratis.Chronicle.Projections.ModelBound" &&
            attribute.Name != "NotProjectedAttribute" && attribute.Name != "NotRewindableAttribute" && attribute.Name != "NestedAttribute");

    static ImmutableHashSet<INamedTypeSymbol> ProjectionAndReducerTargets(Compilation compilation) =>
        compilation.GetSymbolsWithName(_ => true, SymbolFilter.Type)
            .OfType<INamedTypeSymbol>()
            .SelectMany(backing => backing.AllInterfaces)
            .Where(_ => _.IsGenericType && _.TypeArguments.Length == 1 &&
                (_.OriginalDefinition.Name == "IProjectionFor" || _.OriginalDefinition.Name == "IReducerFor") &&
                (_.ContainingNamespace.ToDisplayString() == "Cratis.Chronicle.Projections" || _.ContainingNamespace.ToDisplayString() == "Cratis.Chronicle.Reducers"))
            .Select(_ => _.TypeArguments[0])
            .OfType<INamedTypeSymbol>()
            .ToImmutableHashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);

    static bool IsImmediateAppend(InvocationExpressionSyntax syntax, IMethodSymbol method, SemanticModel model, CancellationToken cancellationToken)
    {
        if (!method.Name.StartsWith("Append", StringComparison.Ordinal) ||
            syntax.Expression is not MemberAccessExpressionSyntax member)
        {
            return false;
        }
        return model.GetTypeInfo(member.Expression, cancellationToken).Type is { } receiver && Implements(receiver, EventLog) &&
            (method.ContainingType.ToDisplayString() == "Cratis.Chronicle.EventSequences.IEventSequence" || Implements(method.ContainingType, EventLog));
    }

    static bool Implements(ITypeSymbol type, string fullName) =>
        type is ITypeParameterSymbol parameter
            ? parameter.ConstraintTypes.Any(_ => Implements(_, fullName))
            : type.ToDisplayString() == fullName || type.AllInterfaces.Any(_ => _.ToDisplayString() == fullName);
}
