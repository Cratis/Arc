// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Security.Claims;

namespace Cratis.Arc.Tenancy.for_TenantMembershipVerification.given;

/// <summary>A membership hook that records each check it receives, never calling a substitute.</summary>
public sealed class RecordingMembership : ITenantMembership
{
    /// <summary>Gets the principal name and tenant of every check, in arrival order.</summary>
    public ConcurrentQueue<(string? Principal, string Tenant)> Calls { get; } = new();

    /// <summary>Gets or sets whether the checks are allowed.</summary>
    public bool Allow { get; set; } = true;

    /// <inheritdoc/>
    public bool IsMember(ClaimsPrincipal principal, TenantId tenantId)
    {
        Calls.Enqueue((principal.Identity?.Name, tenantId.Value));
        return Allow;
    }
}
