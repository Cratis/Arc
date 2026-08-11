// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Hosting;

namespace Cratis.Arc.Tenancy.for_TenancyOptionsValidator.when_starting_the_host;

/// <summary>
/// Selecting the subdomain resolver in configuration and leaving the base domain out is the shape an operator is most
/// likely to ship, because it looks complete - every other resolver has a working default. It resolves no tenant from
/// any host, so every request would fall through to the header a client sets.
/// </summary>
public class and_subdomain_tenancy_has_no_base_domain : Specification
{
    IHost _host;
    Exception _exception;

    void Establish() => _host = new HostBuilder()
        .AddCratisArcCore(options => options.Tenancy.ResolverType = TenantResolverType.Subdomain)
        .Build();

    async Task Because() => _exception = await Catch.Exception(() => _host.StartAsync());

    void Destroy() => _host.Dispose();

    [Fact] void should_refuse_to_start() => _exception.ShouldBeOfExactType<BaseDomainIsNotADomainName>();
}
