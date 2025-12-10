// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Tenancy;

/// <summary>
/// Represents the concept of a tenant identifier.
/// </summary>
/// <param name="Value">The inner value.</param>
public record TenantId(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Implicitly convert from a <see cref="string"/> to a <see cref="TenantId"/>.
    /// </summary>
    /// <param name="value">Value to convert from.</param>
    public static implicit operator TenantId(string value) => new(value);
}
