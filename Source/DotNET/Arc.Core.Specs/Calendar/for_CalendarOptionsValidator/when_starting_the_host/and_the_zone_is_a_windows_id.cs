// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Hosting;

namespace Cratis.Arc.Calendar.for_CalendarOptionsValidator.when_starting_the_host;

/// <summary>
/// A Windows-only id resolves differently per operating system, so it is refused everywhere.
/// </summary>
public class and_the_zone_is_a_windows_id : Specification
{
    IHost _host;
    Exception _exception;

    void Establish() => _host = new HostBuilder()
        .AddCratisArcCore(options => options.Calendar.TimeZone = "W. Europe Standard Time")
        .Build();

    async Task Because() => _exception = await Catch.Exception(() => _host.StartAsync());

    void Destroy() => _host.Dispose();

    [Fact] void should_refuse_to_start() => _exception.ShouldBeOfExactType<TimeZoneIsNotAnIanaZone>();
    [Fact] void should_name_the_id() => _exception.Message.ShouldContain("W. Europe Standard Time");
}
