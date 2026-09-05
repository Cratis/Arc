// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Tenancy.for_SubdomainTenantIdResolver.when_resolving;

/// <summary>
/// The guard that refuses a base domain no host could be matched against runs once, when the resolver is created, so
/// the resolver has to keep resolving against the base domain that guard accepted. A resolver that read
/// <see cref="TenancyOptions.BaseDomain"/> again on each request would pass an address literal written afterwards
/// straight through - no host would match it any more, and every request would take its tenant from the header a
/// client sets. This is the positive form of that: the tenant still resolves from the base domain the resolver was
/// built with, so the later write changed nothing.
/// </summary>
public class and_the_base_domain_is_changed_after_the_resolver_was_created : given.a_subdomain_tenant_id_resolver
{
    string _result;

    void Establish()
    {
        _arcOptions.Tenancy.BaseDomain = "192.168.1.10";
        _context.Host.Returns($"acme.{BaseDomain}");
    }

    void Because() => _result = _resolver.Resolve();

    [Fact] void should_resolve_the_tenant_against_the_base_domain_it_was_created_with() => _result.ShouldEqual("acme");
}
