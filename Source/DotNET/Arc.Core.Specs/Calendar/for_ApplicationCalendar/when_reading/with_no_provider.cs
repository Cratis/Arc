// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Tenancy;

namespace Cratis.Arc.Calendar.for_ApplicationCalendar.when_reading;

/// <summary>
/// Without an operation or tenant zone the application zone applies.
/// </summary>
public class with_no_provider : given.an_application_calendar
{
    ClockReading _reading;
    Exception _exception;

    void Establish()
    {
        _applicationZone = "America/New_York";
        CreateCalendar();
    }

    async Task Because() => _exception = await Catch.Exception(async () => _reading = await _calendar.Read(Instant, new TenantId("acme"), null, CancellationToken.None));

    [Fact] void should_use_the_application_zone() => _reading.Zone.Id.ShouldEqual("America/New_York");
}
