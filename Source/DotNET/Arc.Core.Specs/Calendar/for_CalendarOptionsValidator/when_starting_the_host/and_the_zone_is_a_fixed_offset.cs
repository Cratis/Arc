// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Hosting;

namespace Cratis.Arc.Calendar.for_CalendarOptionsValidator.when_starting_the_host;

/// <summary>
/// A fixed-offset id passes the IANA check but is not a place with a calendar, so it is refused.
/// </summary>
public class and_the_zone_is_a_fixed_offset : Specification
{
    IHost _host;
    Exception _exception;

    void Establish() => _host = new HostBuilder()
        .AddCratisArcCore(options => options.Calendar.TimeZone = "Etc/GMT+5")
        .Build();

    async Task Because() => _exception = await Catch.Exception(() => _host.StartAsync());

    void Destroy() => _host.Dispose();

    [Fact] void should_refuse_to_start() => _exception.ShouldBeOfExactType<TimeZoneIsNotAnIanaZone>();
    [Fact] void should_name_the_id() => _exception.Message.ShouldContain("Etc/GMT+5");
}
