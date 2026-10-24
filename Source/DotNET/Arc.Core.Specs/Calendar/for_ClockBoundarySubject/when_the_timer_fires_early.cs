// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Calendar.for_ClockBoundarySubject;

/// <summary>
/// A fire before the boundary, because the clock moved back, re-arms and emits nothing.
/// </summary>
public class when_the_timer_fires_early : given.a_clock_boundary_subject
{
    void Establish()
    {
        _compute = (candidate, reading) => candidate + (int)(reading.Instant - Start).TotalHours;
        _nextBoundary = reading => reading.Instant < Start.AddHours(1) ? Start.AddHours(1) : null;
        Subscribe();
        _candidates.OnNext(1);
        _time.Skew = TimeSpan.FromHours(-2);
    }

    void Because() => _time.Advance(TimeSpan.FromHours(1));

    [Fact] void should_not_evaluate() => _evaluations.ShouldEqual(1);
    [Fact] void should_not_emit() => Results.ShouldContainOnly(1);
    [Fact] void should_keep_one_timer() => (_time.TimersCreated - _time.TimersDisposed).ShouldEqual(1);
}
