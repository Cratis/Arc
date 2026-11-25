// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission.Naming;
using Cratis.Arc.Screenplay.Emission.Slices;
using Cratis.Arc.Screenplay.Model;
using Cratis.Arc.Screenplay.Model.Graphs;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Arc.Screenplay.Emission.Graphs;

/// <summary>Adds graph declarations to their canonical slice without relying on query or command content.</summary>
/// <param name="naming">The naming policy.</param>
/// <param name="diagnostics">Where version losses are reported.</param>
/// <param name="slices">The application's declaring slices.</param>
public class GraphSliceSyntaxBuilder(IScreenplayNaming naming, ScreenplayDiagnostics diagnostics, IEnumerable<SliceModel> slices)
{
    readonly GraphProjectionNames _projectionNames = new(naming, diagnostics, slices);

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

        var observed = _projectionNames.ForSlice(location, graphs);

        return slice with
        {
            Graphs = placed.Select(new GraphSyntaxBuilder(naming).Build).ToArray(),
            GraphProjections = observed.Select(candidate => new GraphProjectionSyntaxBuilder(naming, diagnostics).Build(candidate.Graph, candidate.Projection)).OfType<Cratis.Screenplay.Syntax.Projections.GraphProjectionSyntax>().ToArray()
        };
    }

    static bool HasGraphContent(SliceSyntax slice) => slice.Graphs.Any() || slice.GraphProjections.Any();
}
