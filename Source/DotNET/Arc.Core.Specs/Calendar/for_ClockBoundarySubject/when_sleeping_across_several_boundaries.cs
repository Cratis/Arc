// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Calendar.for_ClockBoundarySubject;

/// <summary>
/// A sleep across several boundaries gives one evaluation at the current instant, then re-arms for the next boundary.
/// </summary>
public class when_sleeping_across_several_boundaries : given.a_clock_boundary_subject
{
    void Establish()
    {
        _compute = (_, reading) => reading.Today.DayNumber;
        _nextBoundary = reading => LocalDateChanges.NextLocalDateChange(reading.Zone, reading.Instant);
        Subscribe();
        _candidates.OnNext(0);
    }

    void Because() => _time.SetUtcNow(Start.AddDays(3));

    [Fact] void should_evaluate_once_more() => _evaluations.ShouldEqual(2);
    [Fact] void should_evaluate_at_the_current_instant() => _readings[1].Instant.ShouldEqual(Start.AddDays(3));
    [Fact] void should_emit_the_current_date() => Results[1].ShouldEqual(DateOnly.FromDateTime(Start.AddDays(3).UtcDateTime).DayNumber);
    [Fact] void should_re_arm_for_the_next_boundary() => (_time.TimersCreated - _time.TimersDisposed).ShouldEqual(1);
}
