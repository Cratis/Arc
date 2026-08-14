// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Tenancy.for_SubdomainTenantIdResolver.when_creating;

/// <summary>
/// A single label is not a registrable domain, so it cannot be the domain the application is served from.
/// </summary>
public class and_the_base_domain_has_a_single_label : given.tenancy_that_can_be_configured
{
    Exception _exception;

    void Establish() => _arcOptions.Tenancy.BaseDomain = "localhost";

    void Because() => _exception = Catch.Exception(() => Create());

    [Fact] void should_refuse_to_resolve_tenants_from_the_host() => _exception.ShouldBeOfExactType<BaseDomainIsNotADomainName>();
}
