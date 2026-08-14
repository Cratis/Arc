// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Hosting;

namespace Cratis.Arc.Tenancy.for_TenancyOptionsValidator.when_starting_the_host;

/// <summary>
/// The tenant resolver is built by a factory the first time a request needs one, and nothing constructs a factory
/// registration while the service provider is being built - so without an options validation this misconfiguration
/// would first be seen by a user making a request against a process that had already reported itself started. The
/// host has to refuse to start on it instead.
/// </summary>
public class and_subdomain_tenancy_has_an_address_for_a_base_domain : Specification
{
    IHost _host;
    Exception _exception;

    void Establish() => _host = new HostBuilder()
        .AddCratisArcCore(options =>
        {
            options.Tenancy.ResolverType = TenantResolverType.Subdomain;
            options.Tenancy.BaseDomain = "192.168.1.10";
        })
        .Build();

    async Task Because() => _exception = await Catch.Exception(() => _host.StartAsync());

    void Destroy() => _host.Dispose();

    [Fact] void should_refuse_to_start() => _exception.ShouldBeOfExactType<BaseDomainIsNotADomainName>();
}
