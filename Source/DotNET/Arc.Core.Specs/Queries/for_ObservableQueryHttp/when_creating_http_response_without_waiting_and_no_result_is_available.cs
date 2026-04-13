// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Reactive.Subjects;
using Cratis.Execution;

namespace Cratis.Arc.Queries.for_ObservableQueryHttp;

public class when_creating_http_response_without_waiting_and_no_result_is_available : Specification
{
    QueryContext _queryContext;
    ObservableQueryHttpResponse _response;

    void Establish() => _queryContext = new("TestQuery", CorrelationId.New(), Paging.NotPaged, Sorting.None);

    async Task Because() => _response = await ObservableQueryHttp.CreateResponse(
        _queryContext,
        new Subject<TestData>(),
        new ObservableQueryHttpOptions(false, TimeSpan.FromSeconds(1)),
        CancellationToken.None);

    [Fact] void should_return_accepted_status_code() => _response.StatusCode.ShouldEqual(HttpStatusCode.Accepted);
    [Fact] void should_return_not_ready_error() => _response.Result.ExceptionMessages.First().ShouldContain("waitForFirstResult=true");

    record TestData(string Value);
}
