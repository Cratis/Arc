// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Tenancy.for_TenantMembershipWiring;

public class when_membership_is_allowed : given.a_host_with_a_request
{
    void Establish() => _allow = true;
    void Because() => ResolveTenant();
    [Fact] void should_return_the_selected_tenant() => _result.ShouldEqual(new TenantId("selected"));
    [Fact] void should_not_throw() => _exception.ShouldBeNull();
}
