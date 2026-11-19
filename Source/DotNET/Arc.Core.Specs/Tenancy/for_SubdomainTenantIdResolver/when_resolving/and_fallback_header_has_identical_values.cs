// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Tenancy.for_SubdomainTenantIdResolver.when_resolving;

public class and_fallback_header_has_identical_values : given.a_subdomain_tenant_id_resolver
{
    string _result;

    void Establish()
    {
        _context.Host.Returns(BaseDomain);
        _headers[FallbackHeaderName] = "acme, acme";
    }

    void Because() => _result = _resolver.Resolve();

    [Fact] void should_return_the_single_tenant() => _result.ShouldEqual("acme");
}
