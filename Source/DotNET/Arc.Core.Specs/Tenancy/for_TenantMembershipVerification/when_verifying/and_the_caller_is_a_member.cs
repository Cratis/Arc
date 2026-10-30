// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Tenancy.for_TenantMembershipVerification.when_verifying;

public class and_the_caller_is_a_member : given.a_tenant_membership_verification
{
    bool _result;
    void Because() => _result = _verification.Verify(_tenant);
    [Fact] void should_verify_membership() => _result.ShouldBeTrue();
    [Fact] void should_use_the_current_principal() => _membership.Received(1).IsMember(_principal, _tenant);
}
