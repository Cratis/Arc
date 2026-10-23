// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Calendar.for_ClockBoundarySubject;

/// <summary>
/// The clock passes the next boundary while the evaluation runs, so the operator evaluates again at once instead of
/// arming a timer measured from the instant the evaluation started.
/// </summary>
public class when_an_evaluation_crosses_its_boundary : given.a_clock_boundary_subject
{
    void Establish()
    {
        _compute = (candidate, reading) =>
        {
            if (reading.Instant == Start)
            {
                _time.Advance(TimeSpan.FromHours(2));
            }

            return candidate + (int)(reading.Instant - Start).TotalHours;
        };
        _nextBoundary = reading => reading.Instant < Start.AddHours(1) ? Start.AddHours(1) : null;
        Subscribe();
    }

    void Because() => _candidates.OnNext(1);

    [Fact] void should_evaluate_again_at_once() => _evaluations.ShouldEqual(2);
    [Fact] void should_evaluate_at_the_current_instant() => _readings[1].Instant.ShouldEqual(Start.AddHours(2));
    [Fact] void should_emit_the_clock_answer() => Results.ShouldContainOnly(1, 3);
}
