// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;

namespace Cratis.Arc.Calendar.for_ClockBoundarySubject;

/// <summary>
/// The context is cancelled from another thread while a candidate replayed during subscription is evaluated, and that
/// evaluation ends the operator. Neither the subscription nor the cancellation waits for the other.
/// </summary>
public class when_cancelled_while_subscribing : given.a_clock_boundary_subject
{
    const int Bound = 10_000;

    readonly ManualResetEventSlim _cancellationCallbacksRan = new(false);
    BehaviorSubject<int> _replaying;
    Task _cancelling;
    bool _cancelledWithoutWaiting;
    bool _subscribedWithoutWaiting;
    bool _cancelledWithinBound;

    void Establish()
    {
        _replaying = new BehaviorSubject<int>(1);
        Build(_replaying, TimeZoneInfo.Utc);

        // Registered before the operator registers its own, so it runs after the operator's callback has returned.
        _cancellation.Token.Register(_cancellationCallbacksRan.Set);
        _compute = (_, _) =>
        {
            _cancelling = Task.Run(_cancellation.Cancel);
            _cancelledWithoutWaiting = _cancellationCallbacksRan.Wait(Bound);
            throw new InvalidOperationException("evaluate failed");
        };
    }

    void Because()
    {
        _subscribedWithoutWaiting = Task.Run(Subscribe).Wait(Bound);
        _cancelledWithinBound = _cancelling?.Wait(Bound) ?? false;
    }

    void Destroy()
    {
        _replaying.Dispose();
        _cancellationCallbacksRan.Dispose();
    }

    [Fact] void should_not_wait_for_the_subscription_to_cancel() => _cancelledWithoutWaiting.ShouldBeTrue();
    [Fact] void should_complete_the_subscription() => _subscribedWithoutWaiting.ShouldBeTrue();
    [Fact] void should_complete_the_cancellation() => _cancelledWithinBound.ShouldBeTrue();
    [Fact] void should_forward_the_error() => _errors.Select(_ => _.Message).ShouldContainOnly("evaluate failed");
    [Fact] void should_release_the_candidates() => _replaying.HasObservers.ShouldBeFalse();
}
