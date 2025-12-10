// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity;

/// <summary>
/// Represents the unique identifier for the identity owned by the identity provider.
/// </summary>
/// <param name="Value">Concept value.</param>
public record IdentityId(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Represents an empty <see cref="IdentityId"/>.
    /// </summary>
    public static readonly IdentityId Empty = new(string.Empty);

    /// <summary>
    /// Implicitly convert from string to <see cref="IdentityId"/>.
    /// </summary>
    /// <param name="value">String to convert from.</param>
    public static implicit operator IdentityId(string value) => new(value);
}
