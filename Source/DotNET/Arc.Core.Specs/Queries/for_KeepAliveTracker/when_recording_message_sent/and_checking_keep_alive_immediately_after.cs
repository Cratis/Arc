// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_KeepAliveTracker.when_recording_message_sent;

public class and_checking_keep_alive_immediately_after : Specification
{
    KeepAliveTracker _tracker;
    bool _result;

    void Establish()
    {
        _tracker = new KeepAliveTracker();

        // Set last message sent to far in the past so ShouldSendKeepAlive would be true before RecordMessageSent
        typeof(KeepAliveTracker)
            .GetField("_lastMessageSent", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .SetValue(_tracker, DateTimeOffset.UtcNow - TimeSpan.FromMinutes(5));
    }

    void Because()
    {
        _tracker.RecordMessageSent();
        _result = _tracker.ShouldSendKeepAlive(TimeSpan.FromSeconds(30));
    }

    [Fact] void should_return_false() => _result.ShouldBeFalse();
}
