// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

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
    const string EventSourceDefinitionAttribute = "Cratis.Chronicle.EventSources.EventSourceAttribute";
    const string EventStreamDefinitionAttribute = "Cratis.Chronicle.EventSources.EventStreamAttribute";
    const string DeclarationAttribute = "Cratis.Arc.Chronicle.Commands.EventSourceAttribute`1";
    const string DefinitionSuffix = "EventSource";

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
            type.Name,
            sourceName,
            replacement));
    }

    static ImmutableDictionary<string, INamedTypeSymbol> DefinitionsIn(INamespaceSymbol root)
    {
        var builder = ImmutableDictionary.CreateBuilder<string, INamedTypeSymbol>(StringComparer.Ordinal);
        var pending = new Stack<INamespaceOrTypeSymbol>();
        pending.Push(root);
        while (pending.Count > 0)
        {
            foreach (var member in pending.Pop().GetMembers())
            {
                if (member is INamespaceSymbol or INamedTypeSymbol)
                {
                    pending.Push((INamespaceOrTypeSymbol)member);
                }

                if (member is INamedTypeSymbol type)
                {
                    var name = NameOf(type);
                    if (name is not null)
                    {
                        // Two definitions with one name is an error Chronicle reports; suggesting either would guess.
                        builder[name] = builder.ContainsKey(name) ? null! : type;
                    }
                }
            }
        }

        return builder.Where(_ => _.Value is not null).ToImmutableDictionary(StringComparer.Ordinal);
    }

    static string? NameOf(INamedTypeSymbol type)
    {
        var attribute = Find(type, EventSourceDefinitionAttribute);
        if (attribute is null)
        {
            return null;
        }

        if (attribute.ConstructorArguments.Length > 0 && attribute.ConstructorArguments[0].Value is string explicitName)
        {
            return explicitName;
        }

        return type.Name.Length > DefinitionSuffix.Length && type.Name.EndsWith(DefinitionSuffix, StringComparison.Ordinal)
            ? type.Name.Substring(0, type.Name.Length - DefinitionSuffix.Length)
            : type.Name;
    }

    static bool DeclaresStream(INamedTypeSymbol definition, string stream) =>
        definition.GetAttributes().Any(_ =>
            FullName(_.AttributeClass) == EventStreamDefinitionAttribute &&
            _.ConstructorArguments.Length > 0 &&
            (_.ConstructorArguments[0].Value as string) == stream);

    static AttributeData? Find(ISymbol symbol, string name) =>
        symbol.GetAttributes().FirstOrDefault(_ => FullName(_.AttributeClass) == name);

    static string FullName(INamedTypeSymbol? type) => type is null ? string.Empty : $"{type.ContainingNamespace.ToDisplayString()}.{type.MetadataName}";
}
