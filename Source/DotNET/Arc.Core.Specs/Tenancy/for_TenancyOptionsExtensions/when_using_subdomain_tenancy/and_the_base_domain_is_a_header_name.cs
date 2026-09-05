// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Tenancy.for_TenancyOptionsExtensions.when_using_subdomain_tenancy;

/// <summary>
/// Removing the header-only overload means an existing <c>UseSubdomainTenancy("X-Tenant-Id")</c> call now passes a
/// header name where a base domain is expected. That has to be loud - a header name is not a domain name, so it is
/// named as the problem at the call site rather than quietly reinterpreted.
/// </summary>
public class and_the_base_domain_is_a_header_name : Specification
{
    ArcOptions _options;
    Exception _exception;

    void Establish() => _options = new ArcOptions();

    void Because() => _exception = Catch.Exception(() => _options.UseSubdomainTenancy("X-Tenant-Id"));

    [Fact] void should_refuse_the_configuration() => _exception.ShouldBeOfExactType<BaseDomainIsNotADomainName>();
    [Fact] void should_not_reinterpret_it_as_a_base_domain() => _options.Tenancy.BaseDomain.ShouldEqual(string.Empty);
    [Fact] void should_not_rename_the_tenant_header() => _options.Tenancy.HttpHeader.ShouldEqual(Constants.DefaultTenantIdHeader);
}
