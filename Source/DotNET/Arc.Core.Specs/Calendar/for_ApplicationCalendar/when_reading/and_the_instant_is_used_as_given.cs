// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Tenancy;

namespace Cratis.Arc.Calendar.for_ApplicationCalendar.when_reading;

/// <summary>
/// Reading pairs the given instant with the zone and never reads a clock.
/// </summary>
public class and_the_instant_is_used_as_given : given.an_application_calendar
{
    ClockReading _reading;
    Exception _exception;

    void Establish()
    {
        _providers.Add(ProviderReturning(null));
        CreateCalendar();
    }

    async Task Because() => _exception = await Catch.Exception(async () => _reading = await _calendar.Read(Instant, new TenantId("acme"), null, CancellationToken.None));

    [Fact] void should_keep_the_given_instant() => _reading.Instant.ShouldEqual(Instant);
}
