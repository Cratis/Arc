// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Arc.Tenancy.for_TenantMembershipVerification.when_verifying_repeatedly;

public class and_a_membership_is_registered : given.a_membership_verification_with_real_scopes
{
    const int Checks = 10_000;
    long _allocated;
    int _resolutions;
    int _checks;

    void Because()
    {
        _root = new ServiceCollection().AddScoped<ITenantMembership>(_ =>
        {
            _resolutions++;
            return new CountingMembership(this);
        }).BuildServiceProvider();
        _requests.Current = Subscription(NewScope().ServiceProvider, "alice");
        var verification = new TenantMembershipVerification(_requests, new given.FixedPrincipalAccessor(Principal("alice")), NullLogger<TenantMembershipVerification>.Instance);
        var tenant = new TenantId("t");
        for (var index = 0; index < 1_000; index++)
        {
            verification.Verify(tenant);
        }

        GC.Collect();
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var index = 0; index < Checks; index++)
        {
            verification.Verify(tenant);
        }

        _allocated = GC.GetAllocatedBytesForCurrentThread() - before;
    }

    [Fact] void should_resolve_once() => _resolutions.ShouldEqual(1);
    [Fact] void should_run_every_check() => _checks.ShouldEqual(Checks + 1_000);
    [Fact] void should_not_allocate_per_check() => _allocated.ShouldEqual(0);

    sealed class CountingMembership(and_a_membership_is_registered owner) : ITenantMembership
    {
        public bool IsMember(ClaimsPrincipal principal, TenantId tenantId)
        {
            owner._checks++;
            return true;
        }
    }
}
