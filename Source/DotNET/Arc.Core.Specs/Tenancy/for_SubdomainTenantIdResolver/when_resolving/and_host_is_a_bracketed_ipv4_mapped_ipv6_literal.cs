// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Tenancy.for_SubdomainTenantIdResolver.when_resolving;

public class and_host_is_a_bracketed_ipv4_mapped_ipv6_literal : given.a_subdomain_tenant_id_resolver
{
    string _result;

    void Establish() => _context.Host.Returns("[::ffff:10.0.0.5]");

    void Because() => _result = _resolver.Resolve();

    [Fact] void should_fall_back_to_the_tenant_header() => _result.ShouldEqual(HeaderTenantId);
}
