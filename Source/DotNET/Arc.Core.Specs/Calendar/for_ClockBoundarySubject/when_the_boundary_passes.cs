// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Calendar.for_ClockBoundarySubject;

/// <summary>
/// Passing the boundary re-evaluates the last candidates once, and emits the changed answer.
/// </summary>
public class when_the_boundary_passes : given.a_clock_boundary_subject
{
    void Establish()
    {
        _compute = (candidate, reading) => candidate + (int)(reading.Instant - Start).TotalHours;
        _nextBoundary = reading => reading.Instant < Start.AddHours(1) ? Start.AddHours(1) : null;
        Subscribe();
        _candidates.OnNext(1);
    }

    void Because() => _time.Advance(TimeSpan.FromHours(1));

    [Fact] void should_evaluate_once_more() => _evaluations.ShouldEqual(2);
    [Fact] void should_emit_exactly_one_clock_answer() => Results.ShouldContainOnly(1, 2);
}
