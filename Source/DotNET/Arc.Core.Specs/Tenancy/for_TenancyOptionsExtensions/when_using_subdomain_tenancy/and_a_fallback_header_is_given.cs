// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Tenancy.for_TenancyOptionsExtensions.when_using_subdomain_tenancy;

public class and_a_fallback_header_is_given : Specification
{
    ArcOptions _options;

    void Establish() => _options = new ArcOptions();

    void Because() => _options.UseSubdomainTenancy("myapp.com", "X-Custom-Tenant");

    [Fact] void should_use_the_first_argument_as_the_base_domain() => _options.Tenancy.BaseDomain.ShouldEqual("myapp.com");
    [Fact] void should_use_the_second_argument_as_the_tenant_header() => _options.Tenancy.HttpHeader.ShouldEqual("X-Custom-Tenant");
    [Fact] void should_select_the_subdomain_resolver() => _options.Tenancy.ResolverType.ShouldEqual(TenantResolverType.Subdomain);
}
