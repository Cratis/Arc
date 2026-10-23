// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Hosting;

namespace Cratis.Arc.Calendar.for_CalendarOptionsValidator.when_starting_the_host;

/// <summary>
/// An id no host knows stops the host and names the id; Arc never falls back to UTC.
/// </summary>
public class and_the_zone_is_unknown : Specification
{
    IHost _host;
    Exception _exception;

    void Establish() => _host = new HostBuilder()
        .AddCratisArcCore(options => options.Calendar.TimeZone = "Mars/Olympus_Mons")
        .Build();

    async Task Because() => _exception = await Catch.Exception(() => _host.StartAsync());

    void Destroy() => _host.Dispose();

    [Fact] void should_refuse_to_start() => _exception.ShouldBeOfExactType<TimeZoneIsNotAnIanaZone>();
    [Fact] void should_name_the_id() => _exception.Message.ShouldContain("Mars/Olympus_Mons");
}
