// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Tenancy;

namespace OAuthPatCallerAcceptance;

/// <summary>Checks the synthetic person's current tenant membership independently of token verification.</summary>
/// <param name="store">The synthetic identity store.</param>
public sealed class PersonMembership(SampleIdentityStore store) : ITenantMembership
{
    /// <inheritdoc/>
    public bool IsMember(ClaimsPrincipal principal, TenantId tenantId) => store.MembershipActive && tenantId.Value == "tenant-a";
}
