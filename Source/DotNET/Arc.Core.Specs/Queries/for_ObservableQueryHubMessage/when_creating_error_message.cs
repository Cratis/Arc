// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ObservableQueryHubMessage;

public class when_creating_error_message : Specification
{
    const string QueryId = "my-query";
    const string ErrorMessage = "something went wrong";
    ObservableQueryHubMessage _result;

    void Because() => _result = ObservableQueryHubMessage.CreateError(QueryId, ErrorMessage);

    [Fact] void should_have_error_type() => _result.Type.ShouldEqual(ObservableQueryHubMessageType.Error);
    [Fact] void should_carry_query_id() => _result.QueryId.ShouldEqual(QueryId);
    [Fact] void should_carry_error_message_as_payload() => _result.Payload.ShouldEqual(ErrorMessage);
}
