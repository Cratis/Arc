// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Tenancy.for_SubdomainTenantIdResolver.when_creating;

/// <summary>
/// An address literal is a sequence of label-shaped parts, so the letter-digit-hyphen rule alone would accept it and
/// turn every <c>anything.0.0.5</c> host into a tenant. This is what keeps the address rejection load bearing after
/// the base domain is normalized before it is matched - one method normalizes both operands, and this is the operand
/// that still needs it.
/// </summary>
public class and_the_base_domain_is_an_address : given.tenancy_that_can_be_configured
{
    Exception _exception;

    void Establish() => _arcOptions.Tenancy.BaseDomain = "0.0.5";

    void Because() => _exception = Catch.Exception(() => Create());

    [Fact] void should_refuse_to_resolve_tenants_from_the_host() => _exception.ShouldBeOfExactType<BaseDomainIsNotADomainName>();
}
