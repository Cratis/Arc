// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Tenancy.for_TenantMembershipVerification.when_verifying;

public class and_the_tenant_is_not_set : given.a_tenant_membership_verification
{
    bool _allowed;
    Exception _denied;
    void Because()
    {
        _allowed = _verification.Verify(TenantId.NotSet);
        _membership.IsMember(_principal, TenantId.NotSet).Returns(false);
        _denied = Catch.Exception(() => _verification.Verify(TenantId.NotSet));
    }
    [Fact] void should_allow_the_default_tenant_only_if_the_hook_allows_it() => _allowed.ShouldBeTrue();
    [Fact] void should_deny_the_default_tenant_if_the_hook_denies_it() => _denied.ShouldBeOfExactType<TenantMembershipDenied>();
    [Fact] void should_pass_not_set_to_the_hook() => _membership.Received(2).IsMember(_principal, TenantId.NotSet);
}
