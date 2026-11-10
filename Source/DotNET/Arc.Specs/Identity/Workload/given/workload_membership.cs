// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Tenancy;

namespace Cratis.Arc.Identity.Workload.given;

internal sealed class workload_membership : ITenantMembership
{
    internal int Calls;

    public bool IsMember(ClaimsPrincipal principal, TenantId tenantId)
    {
        Calls++;
        return false;
    }
}
