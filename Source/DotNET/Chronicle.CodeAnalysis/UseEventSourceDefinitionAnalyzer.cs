// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using static Cratis.Arc.Chronicle.CodeAnalysis.EventSourceDefinitionSymbols;

namespace Cratis.Arc.Chronicle.CodeAnalysis;

/// <summary>
/// Suggests declaring an event source definition instead of the legacy event source and stream type attributes when
/// the compilation holds a definition they spell out.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UseEventSourceDefinitionAnalyzer : DiagnosticAnalyzer
{
    const string CommandAttribute = "Cratis.Arc.Commands.ModelBound.CommandAttribute";
    const string AggregateRootInterface = "Cratis.Arc.Chronicle.Aggregates.IAggregateRoot";
    const string EventSourceTypeAttribute = "Cratis.Chronicle.Events.EventSourceTypeAttribute";
    const string EventStreamTypeAttribute = "Cratis.Chronicle.Events.EventStreamTypeAttribute";

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
    [
        DiagnosticDescriptors.ARCCHR0013_UseEventSourceDefinition
    ];

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(start =>
        {
            // The definitions are only collected when a type actually carries a legacy attribute.
            var definitions = new Lazy<ImmutableDictionary<string, INamedTypeSymbol>>(
                () => DefinitionsIn(start.Compilation.Assembly.GlobalNamespace),
                LazyThreadSafetyMode.ExecutionAndPublication);
            start.RegisterSymbolAction(_ => Analyze(_, definitions), SymbolKind.NamedType);
        });
    }

    static void Analyze(SymbolAnalysisContext context, Lazy<ImmutableDictionary<string, INamedTypeSymbol>> definitions)
    {
        var type = (INamedTypeSymbol)context.Symbol;
        var sourceType = Find(type, EventSourceTypeAttribute);
        if (sourceType is null ||
            type.TypeKind != TypeKind.Class ||
            Find(type, DeclarationAttribute) is not null ||
            (Find(type, CommandAttribute) is null && !type.AllInterfaces.Any(_ => FullName(_) == AggregateRootInterface)) ||
            sourceType.ConstructorArguments.Length == 0 ||
            sourceType.ConstructorArguments[0].Value is not string sourceName ||
            !definitions.Value.TryGetValue(sourceName, out var definition))
        {
            return;
        }

        var streamType = Find(type, EventStreamTypeAttribute);
        var streamName = streamType is { ConstructorArguments.Length: > 0 } && streamType.ConstructorArguments[0].Value is string name ? name : null;
        if (streamName is not null && !DeclaresStream(definition, streamName))
        {
            return;
        }

        var replacement = streamName is null
            ? $"[EventSource<{definition.Name}>]"
            : $"[EventSource<{definition.Name}>(\"{streamName}\")]";
        var location = sourceType.ApplicationSyntaxReference?.GetSyntax(context.CancellationToken).GetLocation() ?? type.Locations[0];
        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.ARCCHR0013_UseEventSourceDefinition,
            location,
            ImmutableDictionary<string, string?>.Empty
                .Add("Definition", definition.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))
                .Add("Stream", streamName),
            type.Name,
            sourceName,
            replacement));
    }
}
