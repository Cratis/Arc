// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ObservableQueryHubMessage;

public class when_creating_query_result_message : Specification
{
    const string QueryId = "my-query";
    QueryResult _queryResult;
    ObservableQueryHubMessage _result;

    void Establish() => _queryResult = new QueryResult { Data = "some-data" };

    void Because() => _result = ObservableQueryHubMessage.CreateQueryResult(QueryId, _queryResult);

    [Fact] void should_have_query_result_type() => _result.Type.ShouldEqual(ObservableQueryHubMessageType.QueryResult);
    [Fact] void should_carry_query_id() => _result.QueryId.ShouldEqual(QueryId);
    [Fact] void should_carry_result_as_payload() => _result.Payload.ShouldEqual(_queryResult);
}
