// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive;

namespace Cratis.Arc.Calendar.for_ClockBoundarySubject;

/// <summary>
/// While an epoch evaluates on another thread, the last subscriber leaves and then a new one joins. Both are queued behind
/// the epoch: the leave ends the operator, and the late subscriber is completed instead of being left waiting.
/// </summary>
public class when_a_subscriber_joins_while_the_last_one_leaves : given.a_clock_boundary_subject
{
    const int Bound = 10_000;

    readonly ManualResetEventSlim _entered = new(false);
    readonly ManualResetEventSlim _release = new(false);
    readonly List<int> _lateResults = [];
    IDisposable _late;
    int _lateCompletions;
    bool _leftWithoutWaiting;
    bool _joinedWithoutWaiting;
    bool _epochCompleted;

    void Establish()
    {
        _nextBoundary = reading => reading.Instant < Start.AddHours(1) ? Start.AddHours(1) : null;
        _compute = (candidate, _) =>
        {
            if (candidate == 2)
            {
                _entered.Set();
                _release.Wait(Bound);
            }

            return candidate;
        };
        Subscribe();
        _candidates.OnNext(1);
    }

    void Because()
    {
        var epoch = Task.Run(() => _candidates.OnNext(2));
        _entered.Wait(Bound);
        _leftWithoutWaiting = Task.Run(_subscription.Dispose).Wait(Bound);
        _joinedWithoutWaiting = Task.Run(() => _late = _subject.Subscribe(Observer.Create<int>(
            _lateResults.Add,
            _ => { },
            () => _lateCompletions++))).Wait(Bound);
        _release.Set();
        _epochCompleted = epoch.Wait(Bound);
    }

    void Destroy()
    {
        _late?.Dispose();
        _entered.Dispose();
        _release.Dispose();
    }

    [Fact] void should_not_wait_for_the_epoch_to_leave() => _leftWithoutWaiting.ShouldBeTrue();
    [Fact] void should_not_wait_for_the_epoch_to_join() => _joinedWithoutWaiting.ShouldBeTrue();
    [Fact] void should_complete_the_epoch() => _epochCompleted.ShouldBeTrue();
    [Fact] void should_complete_the_late_subscriber() => _lateCompletions.ShouldEqual(1);
    [Fact] void should_emit_nothing_to_the_late_subscriber() => _lateResults.ShouldBeEmpty();
    [Fact] void should_emit_nothing_more_to_the_subscriber_that_left() => Results.ShouldContainOnly(1);
    [Fact] void should_release_the_candidates() => _candidates.HasObservers.ShouldBeFalse();
    [Fact] void should_dispose_the_timer() => _time.TimersDisposed.ShouldEqual(_time.TimersCreated);
}
