// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Tenancy.for_TenantMembershipStartupWarning;

public class when_membership_is_scoped : given.a_host_with_tenancy
{
    void Establish() => _membership = true;
    Task Because() => StartHost();
    [Fact] void should_start_with_scope_validation() => _exception.ShouldBeNull();
    [Fact] void should_not_warn() => Warnings.ShouldEqual(0);
}
