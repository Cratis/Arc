// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Arc.Tenancy.for_TenantMembershipVerification.when_verifying_repeatedly;

public class and_a_membership_is_registered : given.a_membership_verification_with_real_scopes
{
    const int Checks = 10_000;
    const int Rounds = 5;
    long _allocated;
    int _checks;
    given.NonAllocatingServiceProvider _services;

    void Because()
    {
        _services = new given.NonAllocatingServiceProvider(Substitute.For<IServiceScopeFactory>(), [new CountingMembership(this)]);
        _requests.Current = Subscription(_services, "alice");
        var verification = new TenantMembershipVerification(_requests, new given.FixedPrincipalAccessor(Principal("alice")), NullLogger<TenantMembershipVerification>.Instance);
        var tenant = new TenantId("t");
        for (var index = 0; index < 1_000; index++)
        {
            verification.Verify(tenant);
        }

        GC.Collect();

        // Ignore one-time runtime allocations, but require a completely allocation-free round.
        _allocated = long.MaxValue;
        for (var round = 0; round < Rounds; round++)
        {
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var index = 0; index < Checks; index++)
            {
                verification.Verify(tenant);
            }

            _allocated = Math.Min(_allocated, GC.GetAllocatedBytesForCurrentThread() - before);
        }
    }

    [Fact] void should_resolve_once() => _services.MembershipLookups.ShouldEqual(1);
    [Fact] void should_run_every_check() => _checks.ShouldEqual((Checks * Rounds) + 1_000);
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
