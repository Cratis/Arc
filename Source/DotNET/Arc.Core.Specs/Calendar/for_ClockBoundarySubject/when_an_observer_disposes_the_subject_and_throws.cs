// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive;

namespace Cratis.Arc.Calendar.for_ClockBoundarySubject;

/// <summary>
/// An observer disposes the subject and then throws while the first answer is emitted. The queued end still runs before
/// the exception reaches the producer, so nothing stays attached.
/// </summary>
public class when_an_observer_disposes_the_subject_and_throws : given.a_clock_boundary_subject
{
    Exception _error;

    void Establish()
    {
        _nextBoundary = reading => reading.Instant < Start.AddHours(1) ? Start.AddHours(1) : null;
        _subscription = _subject.Subscribe(Observer.Create<int>(_ =>
        {
            _subject.Dispose();
            throw new InvalidOperationException("observer failed");
        }));
    }

    void Because() => _error = Catch.Exception(() => _candidates.OnNext(1));

    [Fact] void should_propagate_the_observer_failure() => _error.Message.ShouldEqual("observer failed");
    [Fact] void should_release_the_candidates() => _candidates.HasObservers.ShouldBeFalse();
    [Fact] void should_dispose_the_timer() => _time.TimersDisposed.ShouldEqual(_time.TimersCreated);
    [Fact] void should_not_evaluate_after_the_end() => AdvanceAndCount().ShouldEqual(1);

    int AdvanceAndCount()
    {
        _time.Advance(TimeSpan.FromHours(2));
        return _evaluations;
    }
}
