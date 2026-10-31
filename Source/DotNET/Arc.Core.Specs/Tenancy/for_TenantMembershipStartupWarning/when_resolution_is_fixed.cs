// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Tenancy.for_TenantMembershipStartupWarning;

public class when_resolution_is_fixed : given.a_host_with_tenancy
{
    void Establish() => _resolver = TenantResolverType.Fixed;
    Task Because() => StartHost();
    [Fact] void should_start() => _exception.ShouldBeNull();
    [Fact] void should_not_warn() => Warnings.ShouldEqual(0);
}
