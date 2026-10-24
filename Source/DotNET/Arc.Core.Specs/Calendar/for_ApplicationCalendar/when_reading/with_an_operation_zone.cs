// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Tenancy;

namespace Cratis.Arc.Calendar.for_ApplicationCalendar.when_reading;

/// <summary>
/// The operation's own zone wins over the tenant and application zones.
/// </summary>
public class with_an_operation_zone : given.an_application_calendar
{
    ClockReading _reading;
    Exception _exception;

    void Establish()
    {
        _applicationZone = "America/New_York";
        _providers.Add(ProviderReturning("Asia/Tokyo"));
        CreateCalendar();
    }

    async Task Because() => _exception = await Catch.Exception(async () => _reading = await _calendar.Read(Instant, new TenantId("acme"), TimeZoneInfo.FindSystemTimeZoneById("Europe/Oslo"), CancellationToken.None));

    [Fact] void should_use_the_operation_zone() => _reading.Zone.ShouldEqual(TimeZoneInfo.FindSystemTimeZoneById("Europe/Oslo"));
}
