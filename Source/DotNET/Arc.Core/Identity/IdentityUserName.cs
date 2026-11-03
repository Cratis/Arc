// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity;

/// <summary>
/// Represents the user name of the identity, typically the preferred username.
/// </summary>
/// <param name="Value">Concept value.</param>
public record IdentityUserName(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Represents an empty <see cref="IdentityUserName"/>.
    /// </summary>
    public static readonly IdentityUserName Empty = new(string.Empty);

    /// <summary>
    /// Implicitly convert from string to <see cref="IdentityUserName"/>.
    /// </summary>
    /// <param name="value">String to convert from.</param>
    public static implicit operator IdentityUserName(string value) => new(value);
}
