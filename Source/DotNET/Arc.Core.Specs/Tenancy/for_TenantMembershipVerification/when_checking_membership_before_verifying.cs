// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Tenancy.for_TenantMembershipVerification;

public class when_checking_membership_before_verifying : given.a_tenant_membership_verification
{
    bool _has;
    bool _verified;

    void Because()
    {
        _has = _verification.HasMembership;
        _verified = _verification.Verify(_tenant);
    }

    [Fact] void should_report_membership() => _has.ShouldBeTrue();
    [Fact] void should_verify() => _verified.ShouldBeTrue();
    [Fact] void should_resolve_memberships_once() => _services.Received(1).GetService(typeof(IEnumerable<ITenantMembership>));
}
