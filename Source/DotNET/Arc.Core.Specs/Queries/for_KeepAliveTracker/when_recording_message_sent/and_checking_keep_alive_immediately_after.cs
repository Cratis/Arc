// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_KeepAliveTracker.when_recording_message_sent;

public class and_checking_keep_alive_immediately_after : Specification
{
    KeepAliveTracker _tracker;
    bool _result;

    void Establish() =>
        _tracker = new KeepAliveTracker(DateTimeOffset.UtcNow - TimeSpan.FromMinutes(5));

    void Because()
    {
        _tracker.RecordMessageSent();
        _result = _tracker.ShouldSendKeepAlive(TimeSpan.FromSeconds(30));
    }

    [Fact] void should_return_false() => _result.ShouldBeFalse();
}
