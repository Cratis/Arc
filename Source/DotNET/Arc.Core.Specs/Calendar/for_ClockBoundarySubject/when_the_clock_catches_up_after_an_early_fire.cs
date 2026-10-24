// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Calendar.for_ClockBoundarySubject;

/// <summary>
/// After an early fire the re-armed timer evaluates at the boundary.
/// </summary>
public class when_the_clock_catches_up_after_an_early_fire : given.a_clock_boundary_subject
{
    void Establish()
    {
        _compute = (candidate, reading) => candidate + (int)(reading.Instant - Start).TotalHours;
        _nextBoundary = reading => reading.Instant < Start.AddHours(1) ? Start.AddHours(1) : null;
        Subscribe();
        _candidates.OnNext(1);
        _time.Skew = TimeSpan.FromHours(-2);
        _time.Advance(TimeSpan.FromHours(1));
        _time.Skew = TimeSpan.Zero;
    }

    void Because() => _time.Advance(TimeSpan.FromHours(2));

    [Fact] void should_evaluate_once_more() => _evaluations.ShouldEqual(2);
    [Fact] void should_emit_the_clock_answer() => Results.ShouldContainOnly(1, 4);
}
