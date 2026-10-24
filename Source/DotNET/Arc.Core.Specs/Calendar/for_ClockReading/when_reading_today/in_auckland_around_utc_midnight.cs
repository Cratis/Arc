// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Calendar.for_ClockReading.when_reading_today;

public class in_auckland_around_utc_midnight : Specification
{
    TimeZoneInfo _zone;
    DateOnly _before;
    DateOnly _after;

    void Establish() => _zone = TimeZoneInfo.FindSystemTimeZoneById("Pacific/Auckland");

    void Because()
    {
        _before = new ClockReading(new DateTimeOffset(2026, 5, 1, 11, 30, 0, TimeSpan.Zero), _zone).Today;
        _after = new ClockReading(new DateTimeOffset(2026, 5, 1, 12, 30, 0, TimeSpan.Zero), _zone).Today;
    }

    [Fact] void should_be_the_local_date_before_local_midnight() => _before.ShouldEqual(new DateOnly(2026, 5, 1));
    [Fact] void should_be_the_next_local_date_after_local_midnight() => _after.ShouldEqual(new DateOnly(2026, 5, 2));
}
