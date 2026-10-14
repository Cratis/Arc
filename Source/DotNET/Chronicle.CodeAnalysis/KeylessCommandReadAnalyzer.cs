// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cratis.Arc.Chronicle.CodeAnalysis;

/// <summary>
/// Reports Chronicle state dependencies in keyless event-producing commands.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class KeylessCommandReadAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [DiagnosticDescriptors.ARCCHR0025_KeylessCommandRead];

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
        if (!ChronicleStateSymbols.IsCommand(command, context.Compilation) ||
            RawGuidResponseAnalysis.HasRuntimeEventSourceId(command, context.Compilation))
        {
            return;
        }
        var parameters = ChronicleStateSymbols.DependencyMethods(command)
            .Where(method => !ChronicleStateSymbols.MayBeProvided(method, command))
            .SelectMany(method => method.Parameters)
            .Where(parameter => parameter.Locations.Any(location => location.IsInSource) &&
                !ChronicleStateSymbols.IsKeyedService(parameter, context.Compilation));
        foreach (var parameter in parameters)
        {
            var model = ChronicleStateSymbols.IsBacked(parameter.Type, context.Compilation, backedTypes);
            var service = ChronicleStateSymbols.Implements(parameter.Type, "Cratis.Chronicle.ReadModels.IReadModels", context.Compilation) ||
                ChronicleStateSymbols.Implements(parameter.Type, "Cratis.Chronicle.IEventStore", context.Compilation);
            if (!model && !service)
            {
                continue;
            }
            var reason = model
                ? $"read model '{parameter.Type.Name}' is looked up by a newly generated id on every call and will never be found"
                : $"the lookup key used through '{parameter.Type.Name}' is not proven to identify the intended entity";
            context.ReportDiagnostic(ChronicleStateSymbols.Diagnostic(
                DiagnosticDescriptors.ARCCHR0025_KeylessCommandRead,
                parameter,
                model ? DiagnosticSeverity.Warning : DiagnosticSeverity.Info,
                command.Name,
                reason));
        }
    }
}
