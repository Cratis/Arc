// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Arc.Tenancy;

/// <summary>
/// Defines an application-owned tenant membership check for request tenant resolution.
/// </summary>
/// <remarks>
/// Resolution only selects a candidate tenant. Register an implementation to verify that the caller may use it.
/// Implementations must be synchronous and must not perform I/O. Load membership data before entering Arc,
/// or use trusted claims or an in-memory cache. Every registered implementation must allow the tenant.
/// <see cref="TenantId.NotSet"/> is also checked; applications decide whether to permit their default tenant.
/// Background work without a request is not checked. Explicit tenant scopes are trusted application code.
/// </remarks>
public interface ITenantMembership
{
    /// <summary>
    /// Checks whether the principal may use the candidate tenant.
    /// </summary>
    /// <param name="principal">The effective principal, or an unauthenticated principal when none is available.</param>
    /// <param name="tenantId">The candidate tenant, including <see cref="TenantId.NotSet"/>.</param>
    /// <returns>True to allow the tenant; false to deny it.</returns>
    bool IsMember(ClaimsPrincipal principal, TenantId tenantId);
}
