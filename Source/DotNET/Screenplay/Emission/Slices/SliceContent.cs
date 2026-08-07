// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Arc.Screenplay.Emission.Slices;

/// <summary>
/// Determines whether a slice declares anything.
/// </summary>
/// <remarks>
/// A slice can end up with nothing to declare - a read model whose projection could not be expressed, with no query
/// onto it, is the common case. Such a slice carries no information and its header alone is not a valid slice body,
/// so it is dropped rather than emitted.
/// </remarks>
public static class SliceContent
{
    /// <summary>
    /// Determines whether a slice declares nothing at all.
    /// </summary>
    /// <param name="slice">The slice to check.</param>
    /// <returns>True when the slice has no body.</returns>
    public static bool IsEmpty(SliceSyntax slice) =>
        !slice.Events.Any() &&
        !slice.Commands.Any() &&
        !slice.Queries.Any() &&
        !slice.Projections.Any() &&
        !slice.Captures.Any() &&
        !slice.Reactors.Any() &&
        !slice.Screens.Any() &&
        !slice.Constraints.Any() &&
        !slice.Specifications.Any() &&
        slice.Description is null;
}
