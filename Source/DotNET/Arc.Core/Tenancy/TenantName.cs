// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Tenancy;

/// <summary>
/// Represents the concept of a tenant name.
/// </summary>
/// <param name="Value">The inner value.</param>
public record TenantName(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Represents an empty tenant name.
    /// </summary>
    public static readonly TenantName Empty = new(string.Empty);

    /// <summary>
    /// Implicitly convert from a <see cref="string"/> to a <see cref="TenantName"/>.
    /// </summary>
    /// <param name="value">Value to convert from.</param>
    public static implicit operator TenantName(string value) => new(value);
}
