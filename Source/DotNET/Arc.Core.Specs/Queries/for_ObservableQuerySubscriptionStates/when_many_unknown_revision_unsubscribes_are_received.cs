// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ObservableQuerySubscriptionStates;

public class when_many_unknown_revision_unsubscribes_are_received : Specification
{
    readonly DateTimeOffset _now = new(2026, 8, 23, 12, 0, 0, TimeSpan.Zero);
    ObservableQuerySubscriptionStates _states;
    ObservableQuerySubscriptionOperation _activeOperation;

    void Establish()
    {
        _states = new(() => _now);
        _activeOperation = _states.TrySubscribe("active", 1, CancellationToken.None)!;
    }

    void Because()
    {
        for (var index = 0; index < ObservableQuerySubscriptionStates.MaximumRetainedTombstones + 100; index++)
        {
            _states.TryUnsubscribe($"unknown-{index}", 1);
        }
    }

    void Destroy() => _states.Dispose();

    [Fact] void should_enforce_the_tombstone_hard_cap() =>
        _states.Count.ShouldEqual(ObservableQuerySubscriptionStates.MaximumRetainedTombstones + 1);
    [Fact] void should_never_evict_the_active_subscription() => _states.IsCurrent("active", _activeOperation).ShouldBeTrue();
    [Fact] void should_keep_the_active_subscription_count() => _states.ActiveCount.ShouldEqual(1);
}
