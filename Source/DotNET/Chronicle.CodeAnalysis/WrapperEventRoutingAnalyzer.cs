// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using static Cratis.Arc.Chronicle.CodeAnalysis.EventSourceDefinitionSymbols;

namespace Cratis.Arc.Chronicle.CodeAnalysis;

/// <summary>
/// Checks literal wrapper routes against event source definitions and suggests definition routing for legacy strings.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class WrapperEventRoutingAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
    [
        DiagnosticDescriptors.ARCCHR0020_WrapperEventStreamNotDeclared,
        DiagnosticDescriptors.ARCCHR0021_UseEventSourceDefinitionRouting
    ];

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(start =>
        {
            var definitions = new Lazy<ImmutableDictionary<string, INamedTypeSymbol>>(
                () => DefinitionsIn(start.Compilation.Assembly.GlobalNamespace), LazyThreadSafetyMode.ExecutionAndPublication);
            start.RegisterSyntaxNodeAction(node => Analyze(node, definitions), SyntaxKind.ObjectInitializerExpression, SyntaxKind.WithInitializerExpression);
        });
    }

    static void Analyze(SyntaxNodeAnalysisContext context, Lazy<ImmutableDictionary<string, INamedTypeSymbol>> definitions)
    {
        var initializer = (InitializerExpressionSyntax)context.Node;
        if (context.SemanticModel.GetTypeInfo(initializer.Parent!).Type is not INamedTypeSymbol type ||
            FullName(type) != "Cratis.Chronicle.EventSequences.EventForEventSourceId")
        {
            return;
        }

        var routing = new WrapperRoutingInitializer(initializer, context.SemanticModel);
        if (!routing.TryReadString("EventSourceType", out var source) || !routing.TryReadString("EventStreamType", out var stream))
        {
            return;
        }

        if (routing.HasUnknownInheritedRouting())
        {
            return;
        }

        ReportRoute(context, routing, (source, stream), definitions);
    }

    static void ReportRoute(SyntaxNodeAnalysisContext context, WrapperRoutingInitializer routing, (string? Source, string? Stream) literals, Lazy<ImmutableDictionary<string, INamedTypeSymbol>> definitions)
    {
        var declaration = routing.CommandDeclaration();
        var legacy = literals.Source is not null || literals.Stream is not null;
        var definition = ResolveDefinition(routing, literals.Source, DefinitionOf(declaration), definitions);
        if (definition is null || definition.TypeKind == TypeKind.Error)
        {
            return;
        }

        var resolvedStream = StreamOf(routing, legacy, literals.Stream, declaration);
        if (resolvedStream is null)
        {
            return;
        }

        var legacyMessage = LegacyMessage(routing, legacy, IsCommandRoute(definition, resolvedStream, declaration));
        Report(context, definition, (resolvedStream, !legacy || literals.Stream is not null), legacyMessage);
    }

    static INamedTypeSymbol? ResolveDefinition(WrapperRoutingInitializer routing, string? source, INamedTypeSymbol? commandDefinition, Lazy<ImmutableDictionary<string, INamedTypeSymbol>> definitions)
    {
        if (routing.ValueOf("EventSource") is not null)
        {
            return routing.TypedDefinition();
        }

        return source is null ? commandDefinition : definitions.Value.TryGetValue(source, out var definition) ? definition : null;
    }

    static string? StreamOf(WrapperRoutingInitializer routing, bool legacy, string? stream, AttributeData? declaration)
    {
        if (legacy)
        {
            return stream ?? CommandStream(declaration);
        }

        return routing.TryReadString("EventStream", out var typedStream) ? typedStream : null;
    }

    static INamedTypeSymbol? DefinitionOf(AttributeData? declaration) => declaration?.AttributeClass?.TypeArguments.FirstOrDefault() as INamedTypeSymbol;

    static string? CommandStream(AttributeData? declaration) => declaration is { ConstructorArguments.Length: > 0 }
        ? declaration.ConstructorArguments[0].Value as string
        : null;

    static bool IsCommandRoute(INamedTypeSymbol definition, string stream, AttributeData? declaration) =>
        SymbolEqualityComparer.Default.Equals(definition, DefinitionOf(declaration)) && stream == CommandStream(declaration);

    static string? LegacyMessage(WrapperRoutingInitializer routing, bool legacy, bool redundant) =>
        legacy && routing.ValueOf("EventSource") is null
            ? redundant ? "is redundant with the command route and drops the definition" : "drops the definition"
            : null;

    static void Report(SyntaxNodeAnalysisContext context, INamedTypeSymbol definition, (string Name, bool IsWrapperNamed) stream, string? legacyMessage)
    {
        if (!DeclaresStream(definition, stream.Name))
        {
            if (stream.IsWrapperNamed)
            {
                context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.ARCCHR0020_WrapperEventStreamNotDeclared, context.Node.GetLocation(), stream.Name, definition.Name));
            }
        }
        else if (legacyMessage is not null)
        {
            var properties = ImmutableDictionary<string, string?>.Empty
                .Add("Definition", definition.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)).Add("Stream", stream.Name);
            context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.ARCCHR0021_UseEventSourceDefinitionRouting, context.Node.GetLocation(), properties, NameOf(definition), stream.Name, legacyMessage, definition.Name));
        }
    }
}
