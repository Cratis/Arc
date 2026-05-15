// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Tenancy.for_SubdomainTenantIdResolver.when_resolving;

public class and_request_has_subdomain : given.a_subdomain_tenant_id_resolver
{
    string _result;

    void Establish() => _context.Host.Returns("acme.localhost");

    void Because() => _result = _resolver.Resolve();

    [Fact] void should_resolve_tenant_id_from_subdomain() => _result.ShouldEqual("acme");
}
