// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_KeepAliveTracker.when_getting_time_until_next_keep_alive;

public class and_interval_has_elapsed : Specification
{
    KeepAliveTracker _tracker;
    TimeSpan _result;

    void Establish() =>
        _tracker = new KeepAliveTracker(DateTimeOffset.UtcNow - TimeSpan.FromMinutes(5));

    void Because() => _result = _tracker.GetTimeUntilNextKeepAlive(TimeSpan.FromSeconds(30));

    [Fact] void should_report_a_keep_alive_is_due_now() => _result.ShouldEqual(TimeSpan.Zero);
}
