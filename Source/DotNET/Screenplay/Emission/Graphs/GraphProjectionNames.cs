// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission.Naming;
using Cratis.Arc.Screenplay.Emission.Slices;
using Cratis.Arc.Screenplay.Model;
using Cratis.Arc.Screenplay.Model.Graphs;

namespace Cratis.Arc.Screenplay.Emission.Graphs;

/// <summary>Checks actual graph projection names across the entire application.</summary>
/// <param name="naming">The naming policy shared with slice emission.</param>
/// <param name="diagnostics">Where name collisions are reported.</param>
/// <param name="slices">The application's declaring slices.</param>
public class GraphProjectionNames(IScreenplayNaming naming, ScreenplayDiagnostics diagnostics, IEnumerable<SliceModel> slices)
{
    readonly ILookup<string, string> _names = slices.ToLookup(slice => slice.Namespace, slice => SliceSyntaxBuilder.NameOf(slice, naming), StringComparer.Ordinal);

    /// <summary>Finds graphs with an unambiguous projection of the requested name in a placement slice.</summary>
    /// <param name="name">The emitted projection name.</param>
    /// <param name="location">The slice's namespace.</param>
    /// <param name="graphs">All application graphs.</param>
    /// <returns>The graphs with matching, unambiguous projections.</returns>
    public IReadOnlyList<GraphModel> ForSlice(string name, string location, IEnumerable<GraphModel> graphs) =>
        [.. ForSlice(location, graphs).Where(candidate => candidate.Projection.Name == name).Select(candidate => candidate.Graph)];

    /// <summary>Finds the unambiguously named projections for a placement slice.</summary>
    /// <param name="location">The slice's namespace.</param>
    /// <param name="graphs">All application graphs.</param>
    /// <returns>The projections whose names do not collide anywhere in the application.</returns>
    public IReadOnlyList<(GraphModel Graph, GraphProjectionModel Projection)> ForSlice(string location, IEnumerable<GraphModel> graphs)
    {
        var candidates = graphs.SelectMany(graph => Projections(graph).Select(projection => (Graph: graph, Projection: projection)))
            .GroupBy(candidate => candidate.Projection.Name, StringComparer.Ordinal).ToArray();
        var result = new List<(GraphModel Graph, GraphProjectionModel Projection)>();
        foreach (var group in candidates.Where(group => group.Any(candidate => candidate.Projection.Namespace == location)))
        {
            if (group.Count() > 1)
            {
                diagnostics.Warning(ScreenplayDiagnosticCodes.UnrecoverableGraph, $"Several graphs require the canonical projection name '{group.Key}'; their projections are withheld rather than inventing Stage names", location);
            }
            else
            {
                result.Add(group.Single());
            }
        }

        return result;
    }

    IEnumerable<GraphProjectionModel> Projections(GraphModel graph)
    {
        if (graph.HasProjectionDeclarations)
        {
            return graph.Projections;
        }

        return graph.HasProjectionPlacement && graph.Elements.Any(element => element.Handlers.Count > 0)
            ? _names[graph.Namespace].Distinct(StringComparer.Ordinal).Select(name => new GraphProjectionModel(name, graph.Namespace, graph.Elements))
            : [];
    }
}
