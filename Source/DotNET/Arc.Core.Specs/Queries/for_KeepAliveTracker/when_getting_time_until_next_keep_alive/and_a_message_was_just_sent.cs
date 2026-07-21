// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_KeepAliveTracker.when_getting_time_until_next_keep_alive;

public class and_a_message_was_just_sent : Specification
{
    static readonly TimeSpan _interval = TimeSpan.FromSeconds(30);
    KeepAliveTracker _tracker;
    TimeSpan _result;

    void Establish() => _tracker = new KeepAliveTracker(DateTimeOffset.UtcNow);

    void Because() => _result = _tracker.GetTimeUntilNextKeepAlive(_interval);

    [Fact] void should_wait_close_to_a_full_interval() => (_result > _interval - TimeSpan.FromSeconds(1)).ShouldBeTrue();
    [Fact] void should_never_wait_longer_than_the_interval() => (_result <= _interval).ShouldBeTrue();
}
