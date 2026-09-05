// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Hosting;

namespace Cratis.Arc.Tenancy.for_TenancyOptionsValidator.when_starting_the_host;

/// <summary>
/// The base domain is only meaningful to the subdomain resolver, so every other resolver must start without one.
/// Without this, the validation that refuses an unconfigured subdomain resolver would refuse every application that
/// resolves tenants any other way - a far worse failure than the one it exists to prevent, and one no other spec
/// here would name.
/// </summary>
public class and_tenancy_is_resolved_from_the_header : Specification
{
    IHost _host;
    Exception _exception;

    void Establish() => _host = new HostBuilder()
        .AddCratisArcCore(options => options.Tenancy.ResolverType = TenantResolverType.Header)
        .Build();

    async Task Because() => _exception = await Catch.Exception(() => _host.StartAsync());

    async Task Destroy()
    {
        await _host.StopAsync();
        _host.Dispose();
    }

    [Fact] void should_start_without_a_base_domain() => _exception.ShouldBeNull();
}
