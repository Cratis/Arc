// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_KeepAliveTracker.when_checking_should_send_keep_alive;

public class and_interval_has_elapsed : Specification
{
    KeepAliveTracker _tracker;
    bool _result;

    void Establish()
    {
        _tracker = new KeepAliveTracker();

        // Manipulate internal state by recording a message sent far in the past
        typeof(KeepAliveTracker)
            .GetField("_lastMessageSent", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .SetValue(_tracker, DateTimeOffset.UtcNow - TimeSpan.FromMinutes(5));
    }

    void Because() => _result = _tracker.ShouldSendKeepAlive(TimeSpan.FromSeconds(30));

    [Fact] void should_return_true() => _result.ShouldBeTrue();
}
