// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ObservableQueryHubMessage;

public class when_creating_unauthorized_message : Specification
{
    const string QueryId = "my-query";
    ObservableQueryHubMessage _result;

    void Because() => _result = ObservableQueryHubMessage.CreateUnauthorized(QueryId);

    [Fact] void should_have_unauthorized_type() => _result.Type.ShouldEqual(ObservableQueryHubMessageType.Unauthorized);
    [Fact] void should_carry_query_id() => _result.QueryId.ShouldEqual(QueryId);
}
