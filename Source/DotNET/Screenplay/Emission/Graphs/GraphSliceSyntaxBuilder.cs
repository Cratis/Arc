// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission.Naming;
using Cratis.Arc.Screenplay.Emission.Slices;
using Cratis.Arc.Screenplay.Model.Graphs;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Arc.Screenplay.Emission.Graphs;

/// <summary>Adds graph declarations to their canonical slice without relying on query or command content.</summary>
/// <param name="naming">The naming policy.</param>
/// <param name="diagnostics">Where version losses are reported.</param>
public class GraphSliceSyntaxBuilder(IScreenplayNaming naming, ScreenplayDiagnostics diagnostics)
{
    /// <summary>Determines whether a slice has neither ordinary nor graph content.</summary>
    /// <param name="slice">The built slice.</param>
    /// <returns>Whether the slice can be omitted.</returns>
    public static bool IsEmpty(SliceSyntax slice) => SliceContent.IsEmpty(slice) && !HasGraphContent(slice);

    /// <summary>Adds V38 graph syntax, or reports that a version cap prevents it.</summary>
    /// <param name="slice">The built slice.</param>
    /// <param name="location">The slice's declaring namespace.</param>
    /// <param name="graphs">The application's graphs.</param>
    /// <param name="options">The generation options.</param>
    /// <returns>The slice with graph content.</returns>
    public SliceSyntax Build(SliceSyntax slice, string location, IEnumerable<GraphModel> graphs, ScreenplayOptions options)
    {
        var placed = graphs.Where(graph => graph.Namespace == location).ToArray();
        if (options.MaximumExecutableModelVersion is { } maximum && !maximum.IsAtLeast(SemanticVersion.V38))
        {
            foreach (var graph in placed)
            {
                diagnostics.Warning(ScreenplayDiagnosticCodes.GraphRequiresV38, $"The graph '{graph.Name}' requires executable model V38 and is omitted by the version cap", graph.Namespace);
            }

            return slice;
        }

        var observed = placed.Where(graph => graph.Elements.Any(element => element.Handlers.Count > 0)).ToArray();
        if (observed.Length > 1)
        {
            diagnostics.Warning(ScreenplayDiagnosticCodes.UnrecoverableGraph, $"Several graphs require the canonical projection name '{slice.Name}'; their projections are withheld rather than inventing Stage names");
            observed = [];
        }

        return slice with
        {
            Graphs = placed.Select(new GraphSyntaxBuilder(naming).Build).ToArray(),
            GraphProjections = observed.Select(graph => new GraphProjectionSyntaxBuilder(naming, diagnostics).Build(graph, slice.Name)).OfType<Cratis.Screenplay.Syntax.Projections.GraphProjectionSyntax>().ToArray()
        };
    }

    static bool HasGraphContent(SliceSyntax slice) => slice.Graphs.Any() || slice.GraphProjections.Any();
}
