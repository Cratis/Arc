// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Tenancy.for_SubdomainTenantIdResolver.when_creating;

/// <summary>
/// <c>192.168.1.10.</c> is the same address as <c>192.168.1.10</c> - a fully qualified host with the root label
/// written out - but one keystroke longer. Removing the trailing dot is part of normalizing the value, so the address
/// rejection has to run on the normalized form to see it; run before, it sees a string that parses as no address and
/// waves through a base domain that turns every <c>victim.192.168.1.10</c> host into the tenant <c>victim</c>. The
/// ideographic and fullwidth spellings the same normalization folds - <c>192。168。1。10</c>, <c>１９２.168.1.10</c> -
/// arrive at the same place through the punycode conversion.
/// </summary>
public class and_the_base_domain_is_an_address_with_a_trailing_dot : given.tenancy_that_can_be_configured
{
    Exception _exception;

    void Establish() => _arcOptions.Tenancy.BaseDomain = "192.168.1.10.";

    void Because() => _exception = Catch.Exception(() => Create());

    [Fact] void should_refuse_to_resolve_tenants_from_the_host() => _exception.ShouldBeOfExactType<BaseDomainIsNotADomainName>();
}
