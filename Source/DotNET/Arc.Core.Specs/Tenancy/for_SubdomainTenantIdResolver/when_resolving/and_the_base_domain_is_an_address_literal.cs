// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Tenancy.for_SubdomainTenantIdResolver.when_resolving;

/// <summary>
/// An address literal normalizes to nothing, so a suffix built from it without checking would be a bare ".", and a
/// host written with an ideographic full stop ends in exactly that after the punycode conversion. The whole
/// attacker-controlled host would then be handed back as the tenant ID.
/// </summary>
public class and_the_base_domain_is_an_address_literal : given.a_subdomain_tenant_id_resolver
{
    const string HalfwidthIdeographicFullStop = "\uFF61";

    string _result;

    void Establish()
    {
        _arcOptions.Tenancy.BaseDomain = "192.168.1.10";
        _context.Host.Returns($"victim-tenant{HalfwidthIdeographicFullStop}");
    }

    void Because() => _result = _resolver.Resolve();

    [Fact] void should_fall_back_to_the_tenant_header() => _result.ShouldEqual(HeaderTenantId);
    [Fact] void should_not_read_the_host_as_a_tenant() => _result.ShouldNotEqual("victim-tenant");
}
