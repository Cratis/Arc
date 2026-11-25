// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model.Graphs;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Analysis.Graphs;

/// <summary>Decodes Arc markers by metadata name without taking a runtime dependency on Arc.</summary>
/// <param name="types">All source types.</param>
/// <param name="diagnostics">Where invalid declarations are reported.</param>
public class GraphProjectionDeclarationReader(IEnumerable<INamedTypeSymbol> types, ScreenplayDiagnostics diagnostics)
{
    readonly Declaration[] _declarations = types.OrderBy(type => type.ToDisplayString(), StringComparer.Ordinal)
        .SelectMany(type => type.GetAttributes().Where(attribute => attribute.AttributeClass?.FullMetadataName() == GraphNames.ProjectionDeclaration)
            .Select(attribute => new Declaration(type, attribute)))
        .ToArray();

    /// <summary>Reports markers that cannot refer to any recovered graph.</summary>
    /// <param name="graphs">The known graph names.</param>
    public void ReportUnknownGraphs(IEnumerable<string> graphs)
    {
        var known = graphs.ToHashSet(StringComparer.Ordinal);
        foreach (var declaration in _declarations.Where(declaration => !known.Contains(declaration.Graph)))
        {
            diagnostics.Warning(
                ScreenplayDiagnosticCodes.InvalidGraphProjectionDeclaration,
                $"Graph '{declaration.Graph}', marker '{declaration.Marker.ToDisplayString()}': unknown or unresolved graph name",
                declaration.Marker.Namespace());
        }
    }

    /// <summary>Determines whether a graph has explicit metadata, even if invalid.</summary>
    /// <param name="graph">The graph name.</param>
    /// <returns>Whether fallback is forbidden.</returns>
    public bool HasDeclarations(string graph) => _declarations.Any(declaration => declaration.Graph == graph);

    /// <summary>Validates the entire explicit projection set for one graph.</summary>
    /// <param name="graph">The graph name.</param>
    /// <param name="elements">Recovered elements keyed by their exact record symbols.</param>
    /// <returns>Explicit projections, or none when any ownership is invalid.</returns>
    public IReadOnlyList<GraphProjectionModel> Read(string graph, IReadOnlyDictionary<ISymbol, GraphElementModel> elements)
    {
        var validator = new GraphProjectionDeclarationValidator(graph, elements, diagnostics);
        foreach (var declaration in _declarations.Where(declaration => declaration.Graph == graph))
        {
            validator.Add(declaration.Marker, declaration.Attribute, declaration.Name);
        }

        return validator.Complete();
    }

    sealed record Declaration(INamedTypeSymbol Marker, AttributeData Attribute)
    {
        public string Name => Attribute.GetArgument(0) as string ?? string.Empty;
        public string Graph => Attribute.GetArgument(1) as string ?? string.Empty;
    }
}
