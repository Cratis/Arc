// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Tenancy.for_SubdomainTenantIdResolver.when_resolving;

/// <summary>
/// The address rejection is shadowed by the base domain match for every ordinary configuration, so this reaches it
/// directly: with a numeric base domain, an IPv4 literal really does end with "." + base domain, and its leading
/// octet really is a dot-free label. Without the rejection the resolver would hand back "10" as a tenant ID.
/// </summary>
public class and_the_base_domain_is_a_numeric_suffix_of_an_address : given.a_subdomain_tenant_id_resolver
{
    string _result;

    void Establish()
    {
        _arcOptions.UseSubdomainTenancy("0.0.5", FallbackHeaderName);
        _context.Host.Returns("10.0.0.5");
    }

    void Because() => _result = _resolver.Resolve();

    [Fact] void should_fall_back_to_the_tenant_header() => _result.ShouldEqual(HeaderTenantId);
    [Fact] void should_not_read_an_address_octet_as_a_tenant() => _result.ShouldNotEqual("10");
}
