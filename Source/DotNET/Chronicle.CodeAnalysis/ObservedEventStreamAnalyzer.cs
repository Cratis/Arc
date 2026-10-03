// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cratis.Arc.Chronicle.CodeAnalysis;

/// <summary>
/// Reports a reactor or reducer filtered with <c>[FromEventSource&lt;TSource&gt;(stream)]</c> to a stream the event
/// source definition does not declare.
/// </summary>
/// <remarks>
/// Only the attributes of the definition itself are read. Nothing is resolved through the definition or through other
/// definitions, so a definition in a referenced assembly is checked the same way as one in the compilation, and a
/// definition can neither loop the analyzer nor be missed because of the order it is discovered in.
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ObservedEventStreamAnalyzer : DiagnosticAnalyzer
{
    const string FromEventSourceAttribute = "Cratis.Chronicle.EventSources.FromEventSourceAttribute`1";
    const string EventStreamDefinitionAttribute = "Cratis.Chronicle.EventSources.EventStreamAttribute";

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
    [
        DiagnosticDescriptors.ARCCHR0014_ObservedEventStreamNotDeclared
    ];

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(Analyze, SymbolKind.NamedType);
    }

    static void Analyze(SymbolAnalysisContext context)
    {
        var type = (INamedTypeSymbol)context.Symbol;
        foreach (var attribute in type.GetAttributes())
        {
            if (attribute.AttributeClass is not { TypeArguments.Length: 1 } attributeClass ||
                attributeClass.TypeArguments[0] is not INamedTypeSymbol { TypeKind: not TypeKind.Error } definition ||
                FullName(attributeClass.OriginalDefinition) != FromEventSourceAttribute ||
                attribute.ConstructorArguments.Length == 0 ||
                attribute.ConstructorArguments[0].Value is not string stream ||
                DeclaresStream(definition, stream))
            {
                continue;
            }

            var location = attribute.ApplicationSyntaxReference?.GetSyntax(context.CancellationToken).GetLocation() ?? type.Locations[0];
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.ARCCHR0014_ObservedEventStreamNotDeclared,
                location,
                type.Name,
                stream,
                definition.Name));
        }
    }

    static bool DeclaresStream(INamedTypeSymbol definition, string stream) =>
        definition.GetAttributes().Any(_ =>
            FullName(_.AttributeClass) == EventStreamDefinitionAttribute &&
            _.ConstructorArguments.Length > 0 &&
            (_.ConstructorArguments[0].Value as string) == stream);

    static string FullName(INamedTypeSymbol? type) => type is null ? string.Empty : $"{type.ContainingNamespace.ToDisplayString()}.{type.MetadataName}";
}
