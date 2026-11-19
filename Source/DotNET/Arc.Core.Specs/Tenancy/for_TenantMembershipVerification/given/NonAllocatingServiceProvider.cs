// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Tenancy.for_TenantMembershipVerification.given;

/// <summary>
/// A service provider returning precreated instances so a lookup never allocates, unlike a real provider whose
/// background resolver compilation is not synchronized with a spec's warmup.
/// </summary>
/// <param name="scopeFactory">The scope factory returned for <see cref="IServiceScopeFactory"/>.</param>
/// <param name="memberships">The memberships returned for <see cref="IEnumerable{T}"/> of <see cref="ITenantMembership"/>.</param>
public sealed class NonAllocatingServiceProvider(IServiceScopeFactory scopeFactory, ITenantMembership[] memberships) : IServiceProvider
{
    /// <summary>Gets the number of times the memberships were looked up.</summary>
    public int MembershipLookups { get; private set; }

    /// <inheritdoc/>
    public object? GetService(Type serviceType)
    {
        if (serviceType == typeof(IServiceScopeFactory))
        {
            return scopeFactory;
        }

        if (serviceType == typeof(IEnumerable<ITenantMembership>))
        {
            MembershipLookups++;
            return memberships;
        }

        return null;
    }
}
