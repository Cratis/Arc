// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Tenancy.for_TenancyOptionsExtensions.when_using_subdomain_tenancy;

/// <summary>
/// The single-argument call is the one the documentation teaches, and it used to bind to a header-only overload that
/// won the overload resolution tie-break - leaving the base domain empty and renaming the tenant header to
/// <c>myapp.com</c>. This pins the argument to the base domain so that trap can never come back silently.
/// </summary>
public class and_only_a_base_domain_is_given : Specification
{
    ArcOptions _options;

    void Establish() => _options = new ArcOptions();

    void Because() => _options.UseSubdomainTenancy("myapp.com");

    [Fact] void should_use_the_argument_as_the_base_domain() => _options.Tenancy.BaseDomain.ShouldEqual("myapp.com");
    [Fact] void should_leave_the_tenant_header_at_its_default() => _options.Tenancy.HttpHeader.ShouldEqual(Constants.DefaultTenantIdHeader);
    [Fact] void should_select_the_subdomain_resolver() => _options.Tenancy.ResolverType.ShouldEqual(TenantResolverType.Subdomain);
}
