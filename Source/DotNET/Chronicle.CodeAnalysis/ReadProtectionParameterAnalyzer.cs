// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Arc.Commands;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cratis.Arc.Chronicle.CodeAnalysis;

/// <summary>
/// Checks source-visible protected parameter shapes and same-source combinations.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ReadProtectionParameterAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
    [
        DiagnosticDescriptors.ARCCHR0024_ReadCannotBeProtected,
        DiagnosticDescriptors.ARCCHR0033_ReadAndAggregateOnSameSource,
        DiagnosticDescriptors.ARCCHR0034_UnprotectedBodyHasReadParameters,
        DiagnosticDescriptors.ARCCHR0036_ExactScopeCoverageNotProven
    ];

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(start =>
        {
            var backed = new Lazy<ImmutableHashSet<INamedTypeSymbol>>(() => ChronicleStateSymbols.BackedTypes(start.Compilation));
            var backings = new Lazy<INamedTypeSymbol[]>(() => start.Compilation.GetSymbolsWithName(_ => true, SymbolFilter.Type)
                .OfType<INamedTypeSymbol>().Where(type => type.AllInterfaces.Any(contract =>
                    RawGuidResponseAnalysis.IsType(contract, "Cratis.Chronicle.Projections.IProjectionFor`1", start.Compilation) ||
                    RawGuidResponseAnalysis.IsType(contract, "Cratis.Chronicle.Reducers.IReducerFor`1", start.Compilation))).ToArray());
            start.RegisterSymbolAction(symbol => Analyze(symbol, backed, backings), SymbolKind.Parameter);
        });
    }

    static void Analyze(SymbolAnalysisContext context, Lazy<ImmutableHashSet<INamedTypeSymbol>> backed, Lazy<INamedTypeSymbol[]> backings)
    {
        var parameter = (IParameterSymbol)context.Symbol;
        if (!TryProtectedTarget(parameter, context.Compilation, backed, out var target))
        {
            return;
        }
        var (method, command, model) = target;
        var mode = ReadProtectionSymbols.Mode(command);
        if (ShouldSkip(parameter, mode, context.Compilation))
        {
            return;
        }
        if (CommandAttributeSymbols.HasUnprotected(method))
        {
            Report(context, DiagnosticDescriptors.ARCCHR0034_UnprotectedBodyHasReadParameters, parameter, method.Name, parameter.Name);
        }
        if (ChronicleStateSymbols.MayBeProvided(method, command))
        {
            return;
        }
        if (ReadModelRefusalSymbols.Refusal(model, context.Compilation, backings.Value) is { } refusal)
        {
            Report(context, DiagnosticDescriptors.ARCCHR0024_ReadCannotBeProtected, parameter, command.Name, parameter.Name, model.Name, refusal.Reason, refusal.Fix);
            return;
        }
        AnalyzeSameSource(context, parameter, target, mode);
    }

    static bool TryProtectedTarget(
        IParameterSymbol parameter,
        Compilation compilation,
        Lazy<ImmutableHashSet<INamedTypeSymbol>> backed,
        out (IMethodSymbol Method, INamedTypeSymbol Command, INamedTypeSymbol Model) target)
    {
        target = default;
        if (parameter.ContainingSymbol is not IMethodSymbol method || !IsSupportedMethod(method))
        {
            return false;
        }
        var command = ReadProtectionSymbols.CommandFor(method);
        if (command is null || !IsDependencyMethod(method, command))
        {
            return false;
        }
        if (parameter.Type is not INamedTypeSymbol model || !ChronicleStateSymbols.IsBacked(model, compilation, backed))
        {
            return false;
        }
        target = (method, command, model);
        return true;
    }

    static bool IsSupportedMethod(IMethodSymbol method) =>
        !method.IsStatic && method.MethodKind is MethodKind.Ordinary or MethodKind.Constructor;

    static bool IsDependencyMethod(IMethodSymbol method, INamedTypeSymbol command) =>
        SymbolEqualityComparer.Default.Equals(method.ContainingType, command)
            ? ChronicleStateSymbols.DependencyMethods(command).Contains(method, SymbolEqualityComparer.Default)
            : method.MethodKind == MethodKind.Constructor;

    static bool ShouldSkip(IParameterSymbol parameter, int mode, Compilation compilation) =>
        mode == 0 || ReadProtectionSymbols.Advisory(parameter) || ChronicleStateSymbols.IsKeyedService(parameter, compilation);

    static void AnalyzeSameSource(
        SymbolAnalysisContext context,
        IParameterSymbol parameter,
        (IMethodSymbol Method, INamedTypeSymbol Command, INamedTypeSymbol Model) target,
        int mode)
    {
        var (_, command, _) = target;
        var key = ReadProtectionSymbols.Lookup(command, parameter, context.Compilation);
        var commandKey = ChronicleStateSymbols.HasProvider(command, context.Compilation) ? null :
            RawGuidResponseAnalysis.PublicProperties(command).FirstOrDefault(property => ChronicleStateSymbols.IsKey(property, context.Compilation));
        if (!SameSource(key, commandKey))
        {
            return;
        }
        var dependencies = ChronicleStateSymbols.DependencyMethods(command).ToArray();
        var aggregate = FindAggregate(dependencies, command, context.Compilation);
        if (mode == 2 && aggregate is not null)
        {
            Report(context, DiagnosticDescriptors.ARCCHR0033_ReadAndAggregateOnSameSource, parameter, command.Name, aggregate.Name, parameter.Name, key!.Name);
        }
        if (NeedsCoverageAdvice(target, dependencies, context.Compilation))
        {
            Report(context, DiagnosticDescriptors.ARCCHR0036_ExactScopeCoverageNotProven, parameter, command.Name, parameter.Name, key!.Name);
        }
    }

    static bool SameSource(IPropertySymbol? key, IPropertySymbol? commandKey) =>
        key is not null && commandKey is not null && SymbolEqualityComparer.Default.Equals(key, commandKey);

    static IParameterSymbol? FindAggregate(IEnumerable<IMethodSymbol> dependencies, INamedTypeSymbol command, Compilation compilation) =>
        dependencies.Where(dependency => !ChronicleStateSymbols.MayBeProvided(dependency, command))
            .SelectMany(dependency => dependency.Parameters).FirstOrDefault(candidate =>
                ReadProtectionSymbols.Is(candidate.Type, "Cratis.Arc.Chronicle.Aggregates.AggregateRoot", compilation));

    static bool NeedsCoverageAdvice(
        (IMethodSymbol Method, INamedTypeSymbol Command, INamedTypeSymbol Model) target,
        IMethodSymbol[] dependencies,
        Compilation compilation) =>
        IsConstructorStreamDecision(target, dependencies, compilation) || HasUnprovenExactScopes(target, dependencies, compilation);

    static bool IsConstructorStreamDecision(
        (IMethodSymbol Method, INamedTypeSymbol Command, INamedTypeSymbol Model) target,
        IMethodSymbol[] dependencies,
        Compilation compilation) =>
        PerStreamReadModelSymbols.Declaration(target.Model) is not null && HasStreamDecision(dependencies, compilation) &&
        target.Method.MethodKind == MethodKind.Constructor;

    static bool HasStreamDecision(IEnumerable<IMethodSymbol> dependencies, Compilation compilation) =>
        dependencies.Any(dependency => dependency.Parameters.Any(candidate =>
            ReadProtectionSymbols.Is(candidate.Type, "Cratis.Arc.Chronicle.Streams.StreamDecision", compilation)) ||
            ReadProtectionSymbols.Is(RawGuidResponseAnalysis.UnwrapAwaitable(dependency.ReturnType, compilation), "Cratis.Arc.Chronicle.Streams.StreamDecision", compilation));

    static bool HasUnprovenExactScopes(
        (IMethodSymbol Method, INamedTypeSymbol Command, INamedTypeSymbol Model) target,
        IMethodSymbol[] dependencies,
        Compilation compilation) =>
        RawGuidResponseAnalysis.HandleMethods(target.Command).Any(handler => RawGuidResponseAnalysis.HasExactScopes(handler.ReturnType, compilation)) &&
        !IsCommandStreamHandler(target, dependencies, compilation);

    static bool IsCommandStreamHandler(
        (IMethodSymbol Method, INamedTypeSymbol Command, INamedTypeSymbol Model) target,
        IMethodSymbol[] dependencies,
        Compilation compilation) =>
        PerStreamReadModelSymbols.Declaration(target.Model) is not null && target.Method.Name == "Handle" && HasCommandStreamDecision(dependencies, compilation);

    static bool HasCommandStreamDecision(IEnumerable<IMethodSymbol> dependencies, Compilation compilation) => dependencies.Any(dependency =>
        dependency.Name == "Provide" && dependency.DeclaringSyntaxReferences.Any(reference =>
            reference.GetSyntax() is MethodDeclarationSyntax
            {
                ExpressionBody.Expression: InvocationExpressionSyntax
                {
                    Expression: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax receiver } member
                }
            } && member.Name.Identifier.ValueText == "ForCommand" && dependency.Parameters.Any(parameter => parameter.Name == receiver.Identifier.ValueText &&
                ReadProtectionSymbols.Is(parameter.Type, "Cratis.Arc.Chronicle.Streams.IStreamReads", compilation))));

    static void Report(SymbolAnalysisContext context, DiagnosticDescriptor descriptor, IParameterSymbol parameter, params object[] arguments) =>
        context.ReportDiagnostic(Diagnostic.Create(descriptor, parameter.Locations.FirstOrDefault(), arguments));
}
