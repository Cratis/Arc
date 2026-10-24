// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Calendar.for_ClockReading.when_reading_today;

public class in_utc : Specification
{
    DateOnly _result;

    void Because() => _result = new ClockReading(new DateTimeOffset(2026, 5, 1, 23, 59, 59, TimeSpan.Zero), TimeZoneInfo.Utc).Today;

    [Fact] void should_be_the_utc_date() => _result.ShouldEqual(new DateOnly(2026, 5, 1));
}
