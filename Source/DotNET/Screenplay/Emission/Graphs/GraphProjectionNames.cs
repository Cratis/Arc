// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission.Naming;
using Cratis.Arc.Screenplay.Emission.Slices;
using Cratis.Arc.Screenplay.Model;
using Cratis.Arc.Screenplay.Model.Graphs;

namespace Cratis.Arc.Screenplay.Emission.Graphs;

/// <summary>Checks canonical graph projection names across the entire application, not just one slice.</summary>
/// <param name="naming">The naming policy shared with slice emission.</param>
/// <param name="diagnostics">Where canonical-name collisions are reported.</param>
/// <param name="slices">The application's declaring slices.</param>
public class GraphProjectionNames(IScreenplayNaming naming, ScreenplayDiagnostics diagnostics, IEnumerable<SliceModel> slices)
{
    readonly ILookup<string, string> _names = slices.ToLookup(slice => slice.Namespace, slice => SliceSyntaxBuilder.NameOf(slice, naming), StringComparer.Ordinal);

    /// <summary>Finds the unambiguously named projections for a placement slice.</summary>
    /// <param name="name">The emitted slice name.</param>
    /// <param name="location">The slice's namespace.</param>
    /// <param name="graphs">All application graphs.</param>
    /// <returns>The projections whose canonical names do not collide anywhere in the application.</returns>
    public IReadOnlyList<GraphModel> ForSlice(string name, string location, IEnumerable<GraphModel> graphs)
    {
        var candidates = graphs.Where(graph => HasProjection(graph) && _names[graph.Namespace].Contains(name, StringComparer.Ordinal)).ToArray();
        if (!candidates.Any(graph => graph.Namespace == location))
        {
            return [];
        }

        if (candidates.Length > 1)
        {
            diagnostics.Warning(ScreenplayDiagnosticCodes.UnrecoverableGraph, $"Several graphs require the canonical projection name '{name}'; their projections are withheld rather than inventing Stage names", location);
            return [];
        }

        return candidates;
    }

    static bool HasProjection(GraphModel graph) => graph.HasProjectionPlacement && graph.Elements.Any(element => element.Handlers.Count > 0);
}
