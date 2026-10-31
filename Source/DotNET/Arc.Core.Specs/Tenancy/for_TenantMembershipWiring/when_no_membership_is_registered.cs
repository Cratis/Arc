// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Tenancy.for_TenantMembershipWiring;

public class when_no_membership_is_registered : given.a_host_with_a_request
{
    void Establish() => _registerMembership = false;
    void Because() => ResolveTenant();
    [Fact] void should_preserve_unchecked_resolution() => _result.ShouldEqual(new TenantId("selected"));
    [Fact] void should_not_throw() => _exception.ShouldBeNull();
}
