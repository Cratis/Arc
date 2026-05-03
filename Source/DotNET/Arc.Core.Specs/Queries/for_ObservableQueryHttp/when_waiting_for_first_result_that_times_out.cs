// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Reactive.Subjects;
using Cratis.Execution;

namespace Cratis.Arc.Queries.for_ObservableQueryHttp;

public class when_waiting_for_first_result_that_times_out : Specification
{
    QueryContext _queryContext;
    ObservableQueryHttpResponse _response;

    void Establish() => _queryContext = new("TestQuery", CorrelationId.New(), Paging.NotPaged, Sorting.None);

    async Task Because() => _response = await ObservableQueryHttp.CreateResponse(
        _queryContext,
        new Subject<TestData>(),
        new ObservableQueryHttpOptions(true, TimeSpan.FromMilliseconds(25)),
        CancellationToken.None);

    [Fact] void should_return_request_timeout_status_code() => _response.StatusCode.ShouldEqual(HttpStatusCode.RequestTimeout);
    [Fact] void should_return_timeout_error() => _response.Result.ExceptionMessages.First().ShouldContain("Timed out waiting 0.025 seconds");

    record TestData(string Value);
}
