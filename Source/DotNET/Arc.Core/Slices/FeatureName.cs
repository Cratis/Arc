// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Slices;

/// <summary>
/// Represents the feature the slice belongs to, as part of a <see cref="SliceLocation"/>.
/// </summary>
/// <param name="Value">The underlying value; empty when the namespace is too shallow to hold it.</param>
public record FeatureName(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Gets the value used when the namespace is too shallow to hold it.
    /// </summary>
    public static readonly FeatureName NotSet = new(string.Empty);

    /// <summary>
    /// Implicitly converts a <see cref="string"/> to a <see cref="FeatureName"/>.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    public static implicit operator FeatureName(string value) => new(value);
}
