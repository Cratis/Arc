// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Tenancy.for_SubdomainTenantIdResolver.when_resolving;

/// <summary>
/// <c>acme.myapp.com.evil.com</c> contains the base domain but is served from a domain the attacker owns. The base
/// domain has to be the end of the host, not somewhere inside it. Two lines hold this one down - the suffix has to
/// match at the end, and what is left in front of it has to be a single label - so it takes breaking both to make it
/// resolve a tenant, and no assertion here is written to claim otherwise.
/// </summary>
public class and_the_base_domain_is_a_label_of_an_unrelated_domain : given.a_subdomain_tenant_id_resolver
{
    string _result;

    void Establish() => _context.Host.Returns($"acme.{BaseDomain}.evil.com");

    void Because() => _result = _resolver.Resolve();

    [Fact] void should_fall_back_to_the_tenant_header() => _result.ShouldEqual(HeaderTenantId);
}
