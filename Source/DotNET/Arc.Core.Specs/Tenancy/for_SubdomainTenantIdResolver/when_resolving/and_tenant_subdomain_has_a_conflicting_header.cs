// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Tenancy.for_SubdomainTenantIdResolver.when_resolving;

public class and_tenant_subdomain_has_a_conflicting_header : given.a_subdomain_tenant_id_resolver
{
    string _result;

    void Establish()
    {
        _context.Host.Returns($"acme.{BaseDomain}");
        _headers[FallbackHeaderName] = "acme,evil";
    }

    void Because() => _result = _resolver.Resolve();

    [Fact] void should_keep_the_subdomain_tenant() => _result.ShouldEqual("acme");
}
