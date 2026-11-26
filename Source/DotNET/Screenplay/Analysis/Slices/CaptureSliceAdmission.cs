// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.Analysis.Slices;

/// <summary>
/// Keeps recovered captures native only when their whole joined slice is a capture-only inbound translation.
/// </summary>
internal static class CaptureSliceAdmission
{
    /// <summary>
    /// Preserves whole reactions when a joined slice has non-capture behavior.
    /// </summary>
    /// <param name="slices">The joined application slices.</param>
    /// <param name="projects">The projects retaining the original reactor symbols.</param>
    /// <returns>The slices with incompatible captures left in code.</returns>
    internal static IReadOnlyList<SliceModel> Apply(IReadOnlyList<SliceModel> slices, IReadOnlyList<CompilationAnalysis> projects) =>
        [.. slices.Select(slice => ApplyTo(slice, projects))];

    static bool HasOtherBehavior(SliceModel slice) =>
        slice.Commands.Any() || slice.Queries.Any() || slice.Projections.Any() || slice.Reactors.Any() || slice.Constraints.Any();

    static SliceModel ApplyTo(SliceModel slice, IReadOnlyList<CompilationAnalysis> projects)
    {
        if (!slice.Captures.Any() || !HasOtherBehavior(slice))
        {
            return slice;
        }

        // Inbound translation applies to every input of the slice. A local consumer must not acquire that contract
        // merely because it shares a namespace with a capture. Retain all capture code instead of emitting a partial slice.
        return slice with
        {
            Kind = SliceKind.Automation,
            Captures = [],
            Reactors = [.. slice.Reactors.Concat(projects.SelectMany(project => project.CapturedReactors(slice))).DistinctBy(reactor => reactor.Name)]
        };
    }
}
