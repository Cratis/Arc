// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ObservableQueryHubMessage;

public class when_creating_ping_message : Specification
{
    ObservableQueryHubMessage _result;
    long _beforeTimestamp;

    void Establish() => _beforeTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    void Because() => _result = ObservableQueryHubMessage.CreatePing();

    [Fact] void should_have_ping_type() => _result.Type.ShouldEqual(ObservableQueryHubMessageType.Ping);
    [Fact] void should_have_timestamp_close_to_now() => _result.Timestamp!.Value.ShouldBeGreaterThanOrEqual(_beforeTimestamp);
}
