// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Tenancy.for_SubdomainTenantIdResolver.when_creating;

/// <summary>
/// Without a base domain no host resolves a tenant, so every request would take the tenant from a header the client
/// sets. Failing to start says so instead of silently converting host based tenancy into header based tenancy.
/// </summary>
public class and_the_base_domain_is_missing : given.tenancy_that_can_be_configured
{
    Exception _exception;

    void Because() => _exception = Catch.Exception(() => Create());

    [Fact] void should_refuse_to_resolve_tenants_from_the_host() => _exception.ShouldBeOfExactType<BaseDomainIsNotADomainName>();
}
