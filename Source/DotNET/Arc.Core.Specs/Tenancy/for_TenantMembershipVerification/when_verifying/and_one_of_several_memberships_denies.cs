// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Tenancy.for_TenantMembershipVerification.when_verifying;

public class and_one_of_several_memberships_denies : given.a_tenant_membership_verification
{
    Exception _exception;
    void Establish() => _services.GetService(typeof(IEnumerable<ITenantMembership>)).Returns(new[] { _membership, Substitute.For<ITenantMembership>() });
    void Because() => _exception = Catch.Exception(() => _verification.Verify(_tenant));
    [Fact] void should_require_all_memberships_to_allow() => _exception.ShouldBeOfExactType<TenantMembershipDenied>();
}
