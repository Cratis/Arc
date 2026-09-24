// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Tenancy;

namespace Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;

/// <summary>
/// A scoped service that captures the tenant when constructed, revealing accidental reuse of the request scope.
/// </summary>
/// <param name="tenants">The tenant accessor.</param>
public class TenantBoundService(ITenantIdAccessor tenants)
{
    /// <summary>
    /// Gets the tenant bound to this instance.
    /// </summary>
    public TenantId Tenant { get; } = tenants.Current;

    /// <summary>
    /// Gets the unique scoped instance identifier.
    /// </summary>
    public Guid Id { get; } = Guid.NewGuid();
}
