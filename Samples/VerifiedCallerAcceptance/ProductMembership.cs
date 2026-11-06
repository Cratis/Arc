// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Tenancy;

namespace VerifiedCallerAcceptance;

/// <summary>
/// Keeps live tenant membership in the product rather than deriving access from the signed tenant.
/// </summary>
public sealed class ProductMembership : ITenantMembership
{
    /// <inheritdoc/>
    public bool IsMember(ClaimsPrincipal principal, TenantId tenantId) =>
        principal.Identity?.IsAuthenticated == true && tenantId.Value == "tenant-a" &&
        (principal.FindFirst("sub")?.Value == VerifiedCallerAcceptanceSettings.Editor || principal.FindFirst("sub")?.Value == VerifiedCallerAcceptanceSettings.Reader);
}
