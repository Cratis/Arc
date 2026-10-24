// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Cratis.Arc.Calendar.for_CalendarOptionsValidator.when_starting_the_host;

/// <summary>
/// A zone set in code is applied after configuration binding, so it wins over the bound value.
/// </summary>
public class and_the_zone_is_set_in_code_after_configuration : Specification
{
    IHost _host;
    Exception _exception;

    void Establish() => _host = new HostBuilder()
        .ConfigureAppConfiguration(configuration => configuration.AddInMemoryCollection(new Dictionary<string, string?> { ["Cratis:Arc:Calendar:TimeZone"] = "Mars/Olympus_Mons" }))
        .AddCratisArcCore(options => options.Calendar.TimeZone = "Europe/Oslo")
        .Build();

    async Task Because() => _exception = await Catch.Exception(() => _host.StartAsync());

    void Destroy() => _host.Dispose();

    [Fact] void should_start_with_the_zone_set_in_code() => _exception.ShouldBeNull();
}
