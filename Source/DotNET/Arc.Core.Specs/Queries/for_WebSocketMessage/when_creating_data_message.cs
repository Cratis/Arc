// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_WebSocketMessage;

public class when_creating_data_message : Specification
{
    WebSocketMessage _message;
    object _data;

    void Establish()
    {
        _data = new { test = "value" };
    }

    void Because() => _message = WebSocketMessage.CreateData(_data);

    [Fact] void should_have_data_type() => _message.Type.ShouldEqual(WebSocketMessageType.Data);
    [Fact] void should_have_data() => _message.Data.ShouldEqual(_data);
    [Fact] void should_not_have_timestamp() => _message.Timestamp.ShouldBeNull();
}
