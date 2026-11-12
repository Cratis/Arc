// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Slices;

namespace Cratis.Arc.Availability;

/// <summary>
/// Represents a command or read model that is not available to the current context, as clients know it.
/// </summary>
/// <param name="Name">The name of the part: the full name of its command or read model type.</param>
/// <param name="Module">The module of the slice the part belongs to; empty when it has none.</param>
/// <param name="Feature">The feature of the slice the part belongs to; empty when it has none.</param>
/// <param name="Slice">The slice the part belongs to; empty when it has none.</param>
public record UnavailablePart(string Name, string Module, string Feature, string Slice)
{
    /// <summary>
    /// Creates the <see cref="UnavailablePart"/> for a part.
    /// </summary>
    /// <param name="part">The command type, or the type declaring the query.</param>
    /// <returns>The <see cref="UnavailablePart"/>.</returns>
    public static UnavailablePart For(Type part)
    {
        var location = SliceLocation.For(part);
        return new(part.FullName ?? part.Name, location.Module.Value, location.Feature.Value, location.Slice.Value);
    }
}
