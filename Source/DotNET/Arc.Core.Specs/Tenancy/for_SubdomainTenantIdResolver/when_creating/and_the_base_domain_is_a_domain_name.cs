// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Tenancy.for_SubdomainTenantIdResolver.when_creating;

/// <summary>
/// The positive baseline for the fail-closed specs beside it - an ordinary base domain is accepted, so a rejection is
/// never the only outcome the guard can produce.
/// </summary>
public class and_the_base_domain_is_a_domain_name : given.tenancy_that_can_be_configured
{
    Exception _exception;

    void Establish() => _arcOptions.Tenancy.BaseDomain = "myapp.com";

    void Because() => _exception = Catch.Exception(() => Create());

    [Fact] void should_accept_the_configuration() => _exception.ShouldBeNull();
}
