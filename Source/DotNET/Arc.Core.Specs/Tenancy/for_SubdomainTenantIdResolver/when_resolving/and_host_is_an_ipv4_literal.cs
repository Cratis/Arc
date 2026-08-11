// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Tenancy.for_SubdomainTenantIdResolver.when_resolving;

public class and_host_is_an_ipv4_literal : given.a_subdomain_tenant_id_resolver
{
    string _result;

    void Establish() => _context.Host.Returns("10.0.0.5");

    void Because() => _result = _resolver.Resolve();

    [Fact] void should_fall_back_to_the_tenant_header() => _result.ShouldEqual(HeaderTenantId);
}
