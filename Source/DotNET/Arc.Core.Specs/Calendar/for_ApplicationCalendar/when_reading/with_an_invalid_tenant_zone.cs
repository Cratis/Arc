// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Tenancy;

namespace Cratis.Arc.Calendar.for_ApplicationCalendar.when_reading;

/// <summary>
/// An invalid tenant zone is refused, never replaced.
/// </summary>
public class with_an_invalid_tenant_zone : given.an_application_calendar
{
    ClockReading _reading;
    Exception _exception;

    void Establish()
    {
        _providers.Add(ProviderReturning("Etc/GMT+5"));
        CreateCalendar();
    }

    async Task Because() => _exception = await Catch.Exception(async () => _reading = await _calendar.Read(Instant, new TenantId("acme"), null, CancellationToken.None));

    [Fact] void should_throw_the_typed_exception() => _exception.ShouldBeOfExactType<TimeZoneIsNotAnIanaZone>();
    [Fact] void should_not_replace_it_with_utc() => _reading.ShouldBeNull();
}
