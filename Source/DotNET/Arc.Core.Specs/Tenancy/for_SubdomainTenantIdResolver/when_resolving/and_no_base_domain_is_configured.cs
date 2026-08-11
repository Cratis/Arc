// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Tenancy.for_SubdomainTenantIdResolver.when_resolving;

/// <summary>
/// Configuration is refused without a base domain, so this state is only reachable by writing the option after the
/// resolver was built. The host is a single label written with an ideographic full stop, which the punycode
/// conversion turns into a trailing label separator - the shape that matches a bare "." suffix.
/// </summary>
public class and_no_base_domain_is_configured : given.a_subdomain_tenant_id_resolver
{
    const string IdeographicFullStop = "\u3002";

    string _result;

    void Establish()
    {
        _arcOptions.Tenancy.BaseDomain = string.Empty;
        _context.Host.Returns($"attacker{IdeographicFullStop}");
    }

    void Because() => _result = _resolver.Resolve();

    [Fact] void should_fall_back_to_the_tenant_header() => _result.ShouldEqual(HeaderTenantId);
    [Fact] void should_not_read_the_host_as_a_tenant() => _result.ShouldNotEqual("attacker");
}
