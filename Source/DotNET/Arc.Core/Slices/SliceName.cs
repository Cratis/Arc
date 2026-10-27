// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Slices;

/// <summary>
/// Represents the name of a slice, the deepest part of a <see cref="SliceLocation"/>.
/// </summary>
/// <param name="Value">The underlying value; empty when the namespace is too shallow to hold it.</param>
public record SliceName(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Gets the value used when the namespace is too shallow to hold it.
    /// </summary>
    public static readonly SliceName NotSet = new(string.Empty);

    /// <summary>
    /// Implicitly converts a <see cref="string"/> to a <see cref="SliceName"/>.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    public static implicit operator SliceName(string value) => new(value);
}
