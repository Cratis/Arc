// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Tenancy;

namespace Cratis.Arc.Calendar.for_ApplicationCalendar.when_reading;

/// <summary>
/// A failing provider propagates; Arc does not guess a zone.
/// </summary>
public class with_a_failing_provider : given.an_application_calendar
{
    ClockReading _reading;
    Exception _exception;

    void Establish()
    {
        var provider = Substitute.For<ICanProvideTenantCalendar>();
        provider.Provide(Arg.Any<TenantId>(), Arg.Any<CancellationToken>()).Returns<ValueTask<string?>>(_ => throw new InvalidOperationException("provider failed"));
        _providers.Add(provider);
        CreateCalendar();
    }

    async Task Because() => _exception = await Catch.Exception(async () => _reading = await _calendar.Read(Instant, new TenantId("acme"), null, CancellationToken.None));

    [Fact] void should_propagate_the_failure() => _exception.ShouldBeOfExactType<InvalidOperationException>();
}
