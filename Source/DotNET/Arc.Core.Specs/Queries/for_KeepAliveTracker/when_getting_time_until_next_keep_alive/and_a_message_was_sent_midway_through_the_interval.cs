// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_KeepAliveTracker.when_getting_time_until_next_keep_alive;

/// <summary>
/// Guards the regression that caused clients to drop healthy connections: a keep-alive loop waking on a
/// fixed interval grid would skip the tick following a mid-interval data message and defer the keep-alive
/// by a further full interval, letting the gap between messages approach twice the interval. The remaining
/// time must always be measured from the last message sent, so the gap can never exceed one interval.
/// </summary>
public class and_a_message_was_sent_midway_through_the_interval : Specification
{
    static readonly TimeSpan _interval = TimeSpan.FromSeconds(30);
    KeepAliveTracker _tracker;
    TimeSpan _result;

    void Establish() =>
        _tracker = new KeepAliveTracker(DateTimeOffset.UtcNow - TimeSpan.FromSeconds(20));

    void Because() => _result = _tracker.GetTimeUntilNextKeepAlive(_interval);

    [Fact] void should_wait_only_the_remainder_of_the_interval() => (_result <= TimeSpan.FromSeconds(10)).ShouldBeTrue();
    [Fact] void should_not_defer_to_the_next_full_interval() => (_result < _interval).ShouldBeTrue();
    [Fact] void should_still_have_time_remaining() => (_result > TimeSpan.Zero).ShouldBeTrue();
}
