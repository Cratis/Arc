// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Arc.Commands;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cratis.Arc.Chronicle.CodeAnalysis;

/// <summary>
/// Reports read-model parameter lookup keys that cannot be proven from the command signature.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class CommandReadModelLookupAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [DiagnosticDescriptors.ARCCHR0023_ReadModelLookupNotProven];

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(start =>
        {
            var backedTypes = new Lazy<ImmutableHashSet<INamedTypeSymbol>>(() => ChronicleStateSymbols.BackedTypes(start.Compilation));
            start.RegisterSymbolAction(context => AnalyzeCommand(context, backedTypes), SymbolKind.NamedType);
        });
    }

    static void AnalyzeCommand(SymbolAnalysisContext context, Lazy<ImmutableHashSet<INamedTypeSymbol>> backedTypes)
    {
        var command = (INamedTypeSymbol)context.Symbol;
        if (!ChronicleStateSymbols.IsCommand(command, context.Compilation) || ChronicleStateSymbols.HasProvider(command, context.Compilation))
        {
            return;
        }
        var properties = RawGuidResponseAnalysis.PublicProperties(command).ToArray();
        var commandKey = properties.FirstOrDefault(property => ChronicleStateSymbols.IsKey(property, context.Compilation));
        if (commandKey is null)
        {
            return;
        }
        foreach (var method in ChronicleStateSymbols.DependencyMethods(command).Where(method => !ChronicleStateSymbols.MayBeProvided(method, command)))
        {
            AnalyzeParameters(context, command, commandKey, properties, method, backedTypes);
        }
    }

    static void AnalyzeParameters(
        SymbolAnalysisContext context,
        INamedTypeSymbol command,
        IPropertySymbol commandKey,
        IPropertySymbol[] properties,
        IMethodSymbol method,
        Lazy<ImmutableHashSet<INamedTypeSymbol>> backedTypes)
    {
        var resolvedByModel = new Dictionary<ITypeSymbol, Dictionary<IPropertySymbol, IParameterSymbol>>(SymbolEqualityComparer.Default);
        foreach (var parameter in method.Parameters.Where(parameter =>
            parameter.Locations.Any(location => location.IsInSource) &&
            !ChronicleStateSymbols.IsKeyedService(parameter, context.Compilation) &&
            ChronicleStateSymbols.IsBacked(parameter.Type, context.Compilation, backedTypes)))
        {
            if (PerStreamReadModelSymbols.Declaration(parameter.Type) is not null ||
                CommandAttributeSymbols.Find(parameter, "Cratis.Arc.Commands.LookUpByAttribute") is not null)
            {
                continue;
            }
            var modelKey = ChronicleStateSymbols.ModelKey((INamedTypeSymbol)parameter.Type, context.Compilation);
            if (modelKey is null)
            {
                continue;
            }
            var matches = properties.Where(property => !property.IsStatic && !property.IsIndexer &&
                SymbolEqualityComparer.Default.Equals(property.Type, modelKey.Type)).ToArray();
            var distinguishing = ChronicleStateSymbols.IsDistinguishing(modelKey.Type, context.Compilation) &&
                !SymbolEqualityComparer.Default.Equals(modelKey.Type, commandKey.Type);
            if (distinguishing && matches.Length != 1)
            {
                var reason = matches.Length > 1
                    ? $"several properties match '{modelKey.Type.Name}' ({string.Join(", ", matches.Select(property => property.Name))}); the intended property is not proven"
                    : $"no property matches '{modelKey.Type.Name}'; the intended entity is not proven";
                Report(context, parameter, command, matches.Length > 1 ? DiagnosticSeverity.Warning : DiagnosticSeverity.Info, reason);
                continue;
            }
            if (ReadProtectionSymbols.Mode(command) == 2 && matches.Length > 1)
            {
                Report(context, parameter, command, DiagnosticSeverity.Warning, $"several properties match '{modelKey.Type.Name}' ({string.Join(", ", matches.Select(property => property.Name))}); use [LookUpBy(nameof(Command.Property))] to select the intended key");
                continue;
            }
            if (ReadProtectionSymbols.Mode(command) != 2 && distinguishing && matches.Length == 1)
            {
                Report(context, parameter, command, DiagnosticSeverity.Info, $"Off and Measure use command key '{commandKey.Name}', not matching property '{matches[0].Name}'; declare [LookUpBy] to redirect this read");
                continue;
            }
            var lookupKey = ReadProtectionSymbols.Mode(command) == 2 && matches.Length == 1 ? matches[0] : commandKey;
            if (!resolvedByModel.TryGetValue(parameter.Type, out var resolved))
            {
                resolved = new Dictionary<IPropertySymbol, IParameterSymbol>(SymbolEqualityComparer.Default);
                resolvedByModel.Add(parameter.Type, resolved);
            }
            if (resolved.TryGetValue(lookupKey, out var first))
            {
                Report(context, parameter, command, DiagnosticSeverity.Info, $"this parameter and '{first.Name}' infer the same command key '{lookupKey.Name}'; whether this is intentional is not proven");
            }
            else
            {
                resolved.Add(lookupKey, parameter);
            }
        }
    }

    static void Report(SymbolAnalysisContext context, IParameterSymbol parameter, INamedTypeSymbol command, DiagnosticSeverity severity, string reason) =>
        context.ReportDiagnostic(ChronicleStateSymbols.Diagnostic(
            DiagnosticDescriptors.ARCCHR0023_ReadModelLookupNotProven, parameter, severity, parameter.Name, command.Name, reason));
}
