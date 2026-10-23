// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Hosting;

namespace Cratis.Arc.Calendar.for_CalendarOptionsValidator.when_starting_the_host;

/// <summary>
/// The default zone is UTC, which resolves everywhere.
/// </summary>
public class and_the_zone_is_the_default : Specification
{
    IHost _host;
    Exception _exception;

    void Establish() => _host = new HostBuilder()
        .AddCratisArcCore()
        .Build();

    async Task Because() => _exception = await Catch.Exception(() => _host.StartAsync());

    void Destroy() => _host.Dispose();

    [Fact] void should_start() => _exception.ShouldBeNull();
}
