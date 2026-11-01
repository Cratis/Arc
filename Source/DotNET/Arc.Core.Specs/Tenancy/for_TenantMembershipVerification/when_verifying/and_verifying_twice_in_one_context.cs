// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Tenancy.for_TenantMembershipVerification.when_verifying;

public class and_verifying_twice_in_one_context : given.a_tenant_membership_verification
{
    void Because()
    {
        _verification.Verify(_tenant);
        _verification.Verify(_tenant);
    }

    [Fact] void should_resolve_memberships_once() => _services.Received(1).GetService(typeof(IEnumerable<ITenantMembership>));
    [Fact] void should_run_the_check_each_time() => _membership.Received(2).IsMember(_principal, _tenant);
}
