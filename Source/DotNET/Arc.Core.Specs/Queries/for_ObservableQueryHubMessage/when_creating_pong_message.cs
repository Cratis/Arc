// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ObservableQueryHubMessage;

public class when_creating_pong_message : Specification
{
    const long Timestamp = 1740000000000;
    ObservableQueryHubMessage _result;

    void Because() => _result = ObservableQueryHubMessage.CreatePong(Timestamp);

    [Fact] void should_have_pong_type() => _result.Type.ShouldEqual(ObservableQueryHubMessageType.Pong);
    [Fact] void should_echo_provided_timestamp() => _result.Timestamp.ShouldEqual(Timestamp);
}
