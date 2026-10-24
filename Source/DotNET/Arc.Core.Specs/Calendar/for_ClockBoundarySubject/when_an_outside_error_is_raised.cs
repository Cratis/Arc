// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Calendar.for_ClockBoundarySubject;

/// <summary>
/// An error raised from outside, as a failed transport push does, ends the operator.
/// </summary>
public class when_an_outside_error_is_raised : given.a_clock_boundary_subject
{
    void Establish()
    {
        _nextBoundary = reading => reading.Instant < Start.AddHours(1) ? Start.AddHours(1) : null;
        Subscribe();
        _candidates.OnNext(1);
    }

    void Because() => _subject.OnError(new InvalidOperationException("push failed"));

    [Fact] void should_dispose_the_timer() => _time.TimersDisposed.ShouldEqual(_time.TimersCreated);
    [Fact] void should_release_the_candidates() => _candidates.HasObservers.ShouldBeFalse();
    [Fact] void should_not_evaluate_after_the_end() => AdvanceAndCount().ShouldEqual(1);

    int AdvanceAndCount()
    {
        _time.Advance(TimeSpan.FromHours(2));
        return _evaluations;
    }
    [Fact] void should_forward_the_error() => _errors.Count.ShouldEqual(1);
}
