// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Tenancy.for_SubdomainTenantIdResolver.when_resolving;

public class and_base_domain_is_the_www_host : given.a_subdomain_tenant_id_resolver
{
    string _result;

    void Establish()
    {
        _arcOptions.Tenancy.BaseDomain = $"www.{BaseDomain}";
        _context.Host.Returns($"www.{BaseDomain}");
    }

    void Because() => _result = _resolver.Resolve();

    [Fact] void should_fall_back_to_the_tenant_header() => _result.ShouldEqual(HeaderTenantId);
}
