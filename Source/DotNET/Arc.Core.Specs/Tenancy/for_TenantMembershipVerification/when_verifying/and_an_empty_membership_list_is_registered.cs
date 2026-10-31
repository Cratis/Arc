// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Tenancy.for_TenantMembershipVerification.when_verifying;

public class and_an_empty_membership_list_is_registered : given.a_tenant_membership_verification
{
    bool _result;
    void Establish() => _services.GetService(typeof(IEnumerable<ITenantMembership>)).Returns(Array.Empty<ITenantMembership>());
    void Because() => _result = _verification.Verify(_tenant);
    [Fact] void should_preserve_unchecked_resolution() => _result.ShouldBeFalse();
}
