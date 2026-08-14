// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Tenancy.for_SubdomainTenantIdResolver.when_creating;

/// <summary>
/// A bare top level domain would make every registered domain under it a tenant of this application - <c>evil.com</c>
/// would resolve the tenant <c>evil</c>. It is a single label, so the same rule that rejects <c>localhost</c> rejects
/// it; the case is spelled out because it is the one a reader is most likely to think is a real domain.
/// </summary>
public class and_the_base_domain_is_a_bare_top_level_domain : given.tenancy_that_can_be_configured
{
    Exception _exception;

    void Establish() => _arcOptions.Tenancy.BaseDomain = "com";

    void Because() => _exception = Catch.Exception(() => Create());

    [Fact] void should_refuse_to_resolve_tenants_from_the_host() => _exception.ShouldBeOfExactType<BaseDomainIsNotADomainName>();
}
