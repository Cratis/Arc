// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_KeepAliveTracker.when_checking_should_send_keep_alive;

public class and_interval_has_elapsed : Specification
{
    KeepAliveTracker _tracker;
    bool _result;

    void Establish() =>
        _tracker = new KeepAliveTracker(DateTimeOffset.UtcNow - TimeSpan.FromMinutes(5));

    void Because() => _result = _tracker.ShouldSendKeepAlive(TimeSpan.FromSeconds(30));

    [Fact] void should_return_true() => _result.ShouldBeTrue();
}
