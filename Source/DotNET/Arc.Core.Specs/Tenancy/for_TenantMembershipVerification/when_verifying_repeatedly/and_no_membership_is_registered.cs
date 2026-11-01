// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Arc.Tenancy.for_TenantMembershipVerification.when_verifying_repeatedly;

public class and_no_membership_is_registered : given.a_membership_verification_with_real_scopes
{
    const int Checks = 10_000;
    long _allocated;

    void Because()
    {
        _root = new ServiceCollection().BuildServiceProvider();
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

    [Fact] void should_not_allocate_per_check() => _allocated.ShouldEqual(0);
}
