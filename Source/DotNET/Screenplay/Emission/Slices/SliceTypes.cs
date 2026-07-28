// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Syntax;

namespace Cratis.Arc.Screenplay.Emission.Slices;

/// <summary>
/// Converts the kind of a slice into the Screenplay slice type.
/// </summary>
public static class SliceTypes
{
    /// <summary>
    /// Converts a slice kind.
    /// </summary>
    /// <param name="kind">The kind to convert.</param>
    /// <returns>The <see cref="SliceType"/>.</returns>
    public static SliceType Convert(SliceKind kind) => kind switch
    {
        SliceKind.StateChange => SliceType.StateChange,
        SliceKind.Automation => SliceType.Automation,
        SliceKind.Translate => SliceType.Translate,
        _ => SliceType.StateView
    };
}
