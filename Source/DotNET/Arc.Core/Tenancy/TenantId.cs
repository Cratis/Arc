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
    /// Represents a tenant ID that is not set.
    /// </summary>
    public static readonly TenantId NotSet = new("[NotSet]");

    /// <summary>
    /// Represents the default tenant.
    /// </summary>
    /// <remarks>
    /// A host that resolves no tenant and a host that resolves this tenant by name address the same data. Storage
    /// that partitions by tenant must therefore treat the two identically — Chronicle names the default namespace
    /// "Default" and materializes its read models without a tenant suffix, so a reader that suffixes this tenant
    /// resolves a database that does not exist and reads come back empty rather than failing.
    /// </remarks>
    public static readonly TenantId Default = new("Default");

    /// <summary>
    /// Gets a value indicating whether this is the default tenant — either unset, or the default tenant by name.
    /// </summary>
    public bool IsDefault => this == NotSet || this == Default;

    /// <summary>
    /// Implicitly convert from a <see cref="string"/> to a <see cref="TenantId"/>.
    /// </summary>
    /// <param name="value">Value to convert from.</param>
    public static implicit operator TenantId(string value) => new(value);
}
