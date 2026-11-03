// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Tenancy.for_TenantMembershipStartupWarning;

public class when_the_membership_factory_is_not_ready : given.a_host_with_tenancy
{
    void Establish()
    {
        _membership = true;
        _membershipFactoryThrows = true;
    }
    Task Because() => StartHost();
    [Fact] void should_not_construct_the_membership() => _exception.ShouldBeNull();
    [Fact] void should_not_warn() => Warnings.ShouldEqual(0);
}
