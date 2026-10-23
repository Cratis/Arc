// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Calendar.for_ClockReading.when_reading_today;

public class across_a_dst_change : Specification
{
    ClockReading _reading;
    DateOnly _calendarYesterday;
    DateOnly _dateOfInstantMinusOneDay;

    void Establish() => _reading = new ClockReading(
        new DateTimeOffset(2026, 3, 9, 4, 30, 0, TimeSpan.Zero),
        TimeZoneInfo.FindSystemTimeZoneById("America/New_York"));

    void Because()
    {
        _calendarYesterday = _reading.Today.AddDays(-1);
        _dateOfInstantMinusOneDay = _reading.DateOf(_reading.Instant.AddDays(-1));
    }

    [Fact] void should_step_calendar_days_for_today_minus_one() => _calendarYesterday.ShouldEqual(new DateOnly(2026, 3, 8));
    [Fact] void should_step_elapsed_time_for_now_minus_one_day() => _dateOfInstantMinusOneDay.ShouldEqual(new DateOnly(2026, 3, 7));
}
