// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Tenancy;

namespace Cratis.Arc.Calendar.for_ApplicationCalendar.when_reading;

/// <summary>
/// A provider with no zone for the tenant defers to the next provider.
/// </summary>
public class with_a_provider_returning_null : given.an_application_calendar
{
    ClockReading _reading;
    Exception _exception;

    void Establish()
    {
        _applicationZone = "America/New_York";
        _providers.Add(ProviderReturning(null));
        _providers.Add(ProviderReturning("Asia/Tokyo"));
        CreateCalendar();
    }

    async Task Because() => _exception = await Catch.Exception(async () => _reading = await _calendar.Read(Instant, new TenantId("acme"), null, CancellationToken.None));

    [Fact] void should_ask_the_next_provider() => _reading.Zone.Id.ShouldEqual("Asia/Tokyo");
}
