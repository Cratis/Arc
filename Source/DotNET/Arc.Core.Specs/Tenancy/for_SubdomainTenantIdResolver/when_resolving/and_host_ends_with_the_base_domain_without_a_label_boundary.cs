// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Tenancy.for_SubdomainTenantIdResolver.when_resolving;

/// <summary>
/// <c>evil-myapp.com</c> is a domain anyone can register and it ends with the base domain's own text. Only the label
/// boundary in front of the base domain separates it from a tenant host, so the suffix has to be matched with that
/// separator and never as bare text.
/// </summary>
public class and_host_ends_with_the_base_domain_without_a_label_boundary : given.a_subdomain_tenant_id_resolver
{
    string _result;

    void Establish() => _context.Host.Returns($"evil-{BaseDomain}");

    void Because() => _result = _resolver.Resolve();

    [Fact] void should_fall_back_to_the_tenant_header() => _result.ShouldEqual(HeaderTenantId);

    /// <summary>
    /// Restates the assertion above in the terms of the attack, for a reader scanning the spec names. It is implied
    /// by that assertion - the fallback header value and the leading text are different strings, so it cannot fail
    /// while the assertion above passes - and is documentation rather than separate evidence.
    /// </summary>
    [Fact] void should_not_read_the_leading_text_as_a_tenant() => _result.ShouldNotEqual("evil");
}
