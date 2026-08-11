// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Tenancy.for_SubdomainTenantIdResolver.when_resolving;

public class and_host_is_the_base_domain_without_a_fallback_header : given.a_subdomain_tenant_id_resolver
{
    string _result;

    void Establish()
    {
        _context.Host.Returns(BaseDomain);
        _headers.Clear();
    }

    void Because() => _result = _resolver.Resolve();

    [Fact] void should_resolve_no_tenant_id() => _result.ShouldEqual(string.Empty);
}
