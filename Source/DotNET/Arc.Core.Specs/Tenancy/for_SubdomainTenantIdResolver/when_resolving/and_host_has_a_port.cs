// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Tenancy.for_SubdomainTenantIdResolver.when_resolving;

public class and_host_has_a_port : given.a_subdomain_tenant_id_resolver
{
    string _result;

    void Establish() => _context.Host.Returns($"acme.{BaseDomain}:5000");

    void Because() => _result = _resolver.Resolve();

    [Fact] void should_resolve_the_subdomain_as_the_tenant_id() => _result.ShouldEqual("acme");
}
