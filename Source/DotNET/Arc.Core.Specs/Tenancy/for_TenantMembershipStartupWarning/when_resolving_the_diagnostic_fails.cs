// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Tenancy.for_TenantMembershipStartupWarning;

public class when_resolving_the_diagnostic_fails : given.a_host_with_tenancy
{
    void Establish() => _resolverFactoryThrows = true;
    Task Because() => StartHost();
    [Fact] void should_not_prevent_startup() => _exception.ShouldBeNull();
}
