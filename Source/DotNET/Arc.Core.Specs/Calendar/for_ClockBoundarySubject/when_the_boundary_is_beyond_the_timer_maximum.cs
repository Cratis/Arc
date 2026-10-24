// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Calendar.for_ClockBoundarySubject;

/// <summary>
/// A boundary past the timer maximum is reached in steps without evaluating at the intermediate step.
/// </summary>
public class when_the_boundary_is_beyond_the_timer_maximum : given.a_clock_boundary_subject
{
    void Establish()
    {
        _compute = (candidate, reading) => candidate + (int)(reading.Instant - Start).TotalDays;
        _nextBoundary = reading => reading.Instant < Start.AddDays(100) ? Start.AddDays(100) : null;
        Subscribe();
        _candidates.OnNext(1);
    }

    void Because()
    {
        _time.Advance(ClockBoundarySubject<int, int>.MaxTimerDueTime + TimeSpan.FromMinutes(1));
        _time.SetUtcNow(Start.AddDays(100));
    }

    [Fact] void should_evaluate_only_at_the_boundary() => _readings.Select(_ => _.Instant).ShouldContainOnly(Start, Start.AddDays(100));
    [Fact] void should_emit_the_boundary_answer() => Results.ShouldContainOnly(1, 101);
}
