// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_WebSocketMessage;

public class when_creating_pong_message : Specification
{
    WebSocketMessage _message;
    long _timestamp;

    void Establish()
    {
        _timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }

    void Because() => _message = WebSocketMessage.Pong(_timestamp);

    [Fact] void should_have_pong_type() => _message.Type.ShouldEqual(WebSocketMessageType.Pong);
    [Fact] void should_have_timestamp() => _message.Timestamp.ShouldEqual(_timestamp);
    [Fact] void should_not_have_data() => _message.Data.ShouldBeNull();
}
