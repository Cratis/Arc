// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
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
        if (parameter.ContainingSymbol is not IMethodSymbol method ||
            (method.MethodKind != MethodKind.Ordinary && method.MethodKind != MethodKind.Constructor) || method.IsStatic ||
            ReadProtectionSymbols.CommandFor(method) is not { } command ||
            (SymbolEqualityComparer.Default.Equals(method.ContainingType, command) && !ChronicleStateSymbols.DependencyMethods(command).Contains(method, SymbolEqualityComparer.Default)) ||
            (!SymbolEqualityComparer.Default.Equals(method.ContainingType, command) && method.MethodKind != MethodKind.Constructor) ||
            parameter.Type is not INamedTypeSymbol model || !ChronicleStateSymbols.IsBacked(model, context.Compilation, backed))
        {
            return;
        }
        var mode = ReadProtectionSymbols.Mode(command);
        if (mode == 0 || ReadProtectionSymbols.Advisory(parameter) || ChronicleStateSymbols.IsKeyedService(parameter, context.Compilation))
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
        var key = ReadProtectionSymbols.Lookup(command, parameter, context.Compilation);
        var commandKey = ChronicleStateSymbols.HasProvider(command, context.Compilation) ? null :
            RawGuidResponseAnalysis.PublicProperties(command).FirstOrDefault(property => ChronicleStateSymbols.IsKey(property, context.Compilation));
        if (key is null || commandKey is null || !SymbolEqualityComparer.Default.Equals(key, commandKey))
        {
            return;
        }
        var dependencies = ChronicleStateSymbols.DependencyMethods(command).ToArray();
        var aggregate = dependencies.Where(dependency => !ChronicleStateSymbols.MayBeProvided(dependency, command))
            .SelectMany(dependency => dependency.Parameters).FirstOrDefault(candidate =>
                ReadProtectionSymbols.Is(candidate.Type, "Cratis.Arc.Chronicle.Aggregates.AggregateRoot", context.Compilation));
        if (mode == 2 && aggregate is not null)
        {
            Report(context, DiagnosticDescriptors.ARCCHR0033_ReadAndAggregateOnSameSource, parameter, command.Name, aggregate.Name, parameter.Name, key.Name);
        }
        if (RawGuidResponseAnalysis.HandleMethods(command).Any(handler => HasExactScopes(handler.ReturnType, context.Compilation)))
        {
            Report(context, DiagnosticDescriptors.ARCCHR0036_ExactScopeCoverageNotProven, parameter, command.Name, parameter.Name, key.Name);
        }
    }

    static bool HasExactScopes(ITypeSymbol type, Compilation compilation)
    {
        type = RawGuidResponseAnalysis.UnwrapAwaitable(type, compilation);
        if (RawGuidResponseAnalysis.IsType(type, "Cratis.Chronicle.EventSequences.EventsWithConcurrencyScopes", compilation))
        {
            return true;
        }
        if (type is INamedTypeSymbol { IsTupleType: true } tuple)
        {
            return tuple.TupleElements.Any(element => HasExactScopes(element.Type, compilation));
        }
        var branches = RawGuidResponseAnalysis.UnionBranches(type, compilation).ToArray();
        return branches.Length > 1 && branches.Any(branch => HasExactScopes(branch, compilation));
    }

    static void Report(SymbolAnalysisContext context, DiagnosticDescriptor descriptor, IParameterSymbol parameter, params object[] arguments) =>
        context.ReportDiagnostic(Diagnostic.Create(descriptor, parameter.Locations.FirstOrDefault(), arguments));
}
