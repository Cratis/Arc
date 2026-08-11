// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Tenancy.for_SubdomainTenantIdResolver.when_resolving;

public class and_no_base_domain_is_configured : given.a_subdomain_tenant_id_resolver
{
    string _result;

    void Establish()
    {
        var arcOptions = new ArcOptions();
        arcOptions.UseSubdomainTenancy(FallbackHeaderName);
        _options.Value.Returns(arcOptions);
        _context.Host.Returns("acme.localhost");
    }

    void Because() => _result = _resolver.Resolve();

    [Fact] void should_fall_back_to_the_tenant_header() => _result.ShouldEqual(HeaderTenantId);
    [Fact] void should_leave_the_base_domain_unconfigured() => _options.Value.Tenancy.BaseDomain.ShouldEqual(string.Empty);
}
