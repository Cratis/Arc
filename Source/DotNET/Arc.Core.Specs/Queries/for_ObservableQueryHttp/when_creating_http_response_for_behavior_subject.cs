// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Reactive.Subjects;
using Cratis.Execution;

namespace Cratis.Arc.Queries.for_ObservableQueryHttp;

public class when_creating_http_response_for_behavior_subject : Specification
{
    QueryContext _queryContext;
    ObservableQueryHttpResponse _response;

    void Establish() => _queryContext = new("TestQuery", CorrelationId.New(), Paging.NotPaged, Sorting.None);

    async Task Because() => _response = await ObservableQueryHttp.CreateResponse(
        _queryContext,
        new BehaviorSubject<TestData>(new("initial")),
        new ObservableQueryHttpOptions(false, TimeSpan.FromSeconds(1)),
        CancellationToken.None);

    [Fact] void should_return_ok_status_code() => _response.StatusCode.ShouldEqual(HttpStatusCode.OK);
    [Fact] void should_return_current_data() => ((TestData)_response.Result.Data).Value.ShouldEqual("initial");

    record TestData(string Value);
}
