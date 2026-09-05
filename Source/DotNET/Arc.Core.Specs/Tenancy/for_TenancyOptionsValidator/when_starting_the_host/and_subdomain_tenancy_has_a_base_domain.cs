// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Hosting;

namespace Cratis.Arc.Tenancy.for_TenancyOptionsValidator.when_starting_the_host;

/// <summary>
/// The positive baseline for the two refusals beside it - a configured base domain starts, so refusing to start is
/// never the only outcome the startup validation can produce.
/// </summary>
public class and_subdomain_tenancy_has_a_base_domain : Specification
{
    IHost _host;
    Exception _exception;

    void Establish() => _host = new HostBuilder()
        .AddCratisArcCore(options =>
        {
            options.Tenancy.ResolverType = TenantResolverType.Subdomain;
            options.Tenancy.BaseDomain = "myapp.com";
        })
        .Build();

    async Task Because() => _exception = await Catch.Exception(() => _host.StartAsync());

    async Task Destroy()
    {
        await _host.StopAsync();
        _host.Dispose();
    }

    [Fact] void should_start() => _exception.ShouldBeNull();
}
