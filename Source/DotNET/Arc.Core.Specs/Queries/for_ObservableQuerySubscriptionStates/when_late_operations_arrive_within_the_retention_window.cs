// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ObservableQuerySubscriptionStates;

public class when_late_operations_arrive_within_the_retention_window : Specification
{
    DateTimeOffset _now;
    ObservableQuerySubscriptionStates _states;
    ObservableQuerySubscriptionOperation? _olderOperation;
    ObservableQuerySubscriptionOperation? _equalOperation;
    ObservableQuerySubscriptionOperation? _newerOperation;

    void Establish()
    {
        _now = new DateTimeOffset(2026, 8, 23, 12, 0, 0, TimeSpan.Zero);
        _states = new(() => _now);
        _states.TryUnsubscribe("query", 2);
    }

    void Because()
    {
        _now += ObservableQuerySubscriptionStates.TombstoneRetention - TimeSpan.FromTicks(1);
        _olderOperation = _states.TrySubscribe("query", 1, CancellationToken.None);
        _equalOperation = _states.TrySubscribe("query", 2, CancellationToken.None);
        _newerOperation = _states.TrySubscribe("query", 3, CancellationToken.None);
    }

    void Destroy() => _states.Dispose();

    [Fact] void should_reject_the_older_operation() => _olderOperation.ShouldBeNull();
    [Fact] void should_reject_the_equal_operation() => _equalOperation.ShouldBeNull();
    [Fact] void should_accept_the_newer_operation() => _newerOperation.ShouldNotBeNull();
}
