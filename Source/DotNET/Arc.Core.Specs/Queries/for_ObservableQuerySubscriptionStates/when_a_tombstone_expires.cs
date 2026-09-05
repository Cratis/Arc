// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ObservableQuerySubscriptionStates;

public class when_a_tombstone_expires : Specification
{
    DateTimeOffset _now;
    ObservableQuerySubscriptionStates _states;
    ObservableQuerySubscriptionOperation? _operation;

    void Establish()
    {
        _now = new DateTimeOffset(2026, 8, 23, 12, 0, 0, TimeSpan.Zero);
        _states = new(() => _now);
        _states.TryUnsubscribe("query", 2);
    }

    void Because()
    {
        _now += ObservableQuerySubscriptionStates.TombstoneRetention;
        _operation = _states.TrySubscribe("query", 1, CancellationToken.None);
    }

    void Destroy() => _states.Dispose();

    [Fact] void should_remove_the_expired_tombstone() => _operation.ShouldNotBeNull();
    [Fact] void should_make_the_accepted_subscription_active() => _states.ActiveCount.ShouldEqual(1);
}
