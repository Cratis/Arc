// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ObservableQueryHubMessage;

public class when_creating_connected_message : Specification
{
    const string ConnectionId = "test-connection-id";
    ObservableQueryHubMessage _result;

    void Because() => _result = ObservableQueryHubMessage.CreateConnected(ConnectionId);

    [Fact] void should_have_connected_type() => _result.Type.ShouldEqual(ObservableQueryHubMessageType.Connected);
    [Fact] void should_carry_connection_id_as_payload() => _result.Payload.ShouldEqual(ConnectionId);
}
