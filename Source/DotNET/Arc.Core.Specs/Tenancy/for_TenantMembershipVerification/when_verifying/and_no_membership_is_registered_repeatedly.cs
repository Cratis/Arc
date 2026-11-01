// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Tenancy.for_TenantMembershipVerification.when_verifying;

public class and_no_membership_is_registered_repeatedly : given.a_tenant_membership_verification
{
    bool _first;
    bool _second;

    void Establish() => _services.GetService(typeof(IEnumerable<ITenantMembership>)).Returns(Array.Empty<ITenantMembership>());

    void Because()
    {
        _first = _verification.Verify(_tenant);
        _second = _verification.Verify(_tenant);
    }

    [Fact] void should_apply_no_checks_the_first_time() => _first.ShouldBeFalse();
    [Fact] void should_apply_no_checks_the_second_time() => _second.ShouldBeFalse();
    [Fact] void should_resolve_each_time() => _services.Received(2).GetService(typeof(IEnumerable<ITenantMembership>));
}
