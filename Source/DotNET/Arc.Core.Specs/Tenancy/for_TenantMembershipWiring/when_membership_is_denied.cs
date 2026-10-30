// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Tenancy.for_TenantMembershipWiring;

public class when_membership_is_denied : given.a_host_with_a_request
{
    void Because() => ResolveTenant();
    [Fact] void should_deny_through_the_registered_accessor() => _exception.ShouldBeOfExactType<TenantMembershipDenied>();
    [Fact] void should_share_the_concrete_instance() => _accessor.ShouldBeSame(_concrete);
    [Fact] void should_share_the_scope_instance() => _scope.ShouldBeSame(_concrete);
}
