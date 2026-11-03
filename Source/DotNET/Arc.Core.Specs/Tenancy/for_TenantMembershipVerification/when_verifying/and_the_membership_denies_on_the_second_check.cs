// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Tenancy.for_TenantMembershipVerification.when_verifying;

public class and_the_membership_denies_on_the_second_check : given.a_tenant_membership_verification
{
    bool _first;
    Exception _exception;

    void Establish() => _membership.IsMember(_principal, _tenant).Returns(true, false);

    void Because()
    {
        _first = _verification.Verify(_tenant);
        _exception = Catch.Exception(() => _verification.Verify(_tenant));
    }

    [Fact] void should_allow_the_first_check() => _first.ShouldBeTrue();
    [Fact] void should_deny_the_second_check_despite_reused_instances() => _exception.ShouldBeOfExactType<TenantMembershipDenied>();
}
