// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive;

namespace Cratis.Arc.Calendar.for_ClockBoundarySubject;

/// <summary>
/// An observer pushes a new candidate while the answer for the previous one is emitted. The previous candidate has no
/// boundary and the new one has, so the new candidate's boundary must be the one left armed.
/// </summary>
public class when_an_observer_pushes_a_candidate_with_another_boundary : given.a_clock_boundary_subject
{
    int _evaluating;

    void Establish()
    {
        _compute = (candidate, reading) =>
        {
            _evaluating = candidate;
            return (candidate * 10) + (int)(reading.Instant - Start).TotalHours;
        };
        _nextBoundary = reading => _evaluating == 2 && reading.Instant < Start.AddHours(1) ? Start.AddHours(1) : null;
        _subscription = _subject.Subscribe(Observer.Create<int>(result =>
        {
            _results.Add(result);
            if (result == 10)
            {
                _candidates.OnNext(2);
            }
        }));
    }

    void Because()
    {
        _candidates.OnNext(1);
        _time.Advance(TimeSpan.FromHours(1));
    }

    [Fact] void should_evaluate_the_pushed_candidate_after_the_first() => _evaluations.ShouldEqual(3);
    [Fact] void should_emit_the_boundary_answer_of_the_pushed_candidate() => Results.ShouldContainOnly(10, 20, 21);
}
