// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Tenancy.for_SubdomainTenantIdResolver.when_resolving;

/// <summary>
/// The positive half of <c>and_base_domain_is_the_www_host</c>: with <c>www.myapp.com</c> configured as the base
/// domain, one label in front of it is still a tenant. Nothing counts labels, so a base domain of three labels works
/// the same way one of two does - what the resolver removes is the base domain, not a fixed number of labels.
/// </summary>
public class and_host_is_a_tenant_below_the_www_base_domain : given.a_subdomain_tenant_id_resolver
{
    string _result;

    void Establish()
    {
        ConfigureBaseDomain($"www.{BaseDomain}");
        _context.Host.Returns($"acme.www.{BaseDomain}");
    }

    void Because() => _result = _resolver.Resolve();

    [Fact] void should_resolve_the_label_in_front_of_the_base_domain() => _result.ShouldEqual("acme");
}
