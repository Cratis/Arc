// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity;

/// <summary>
/// Represents the name of the identity. Typically the username.
/// </summary>
/// <param name="Value">Concept value.</param>
public record IdentityName(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Represents an empty <see cref="IdentityName"/>.
    /// </summary>
    public static readonly IdentityName Empty = new(string.Empty);

    /// <summary>
    /// Implicitly convert from string to <see cref="IdentityName"/>.
    /// </summary>
    /// <param name="value">String to convert from.</param>
    public static implicit operator IdentityName(string value) => new(value);
}
