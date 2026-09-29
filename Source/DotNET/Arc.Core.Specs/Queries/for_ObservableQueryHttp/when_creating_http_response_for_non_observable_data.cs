// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Execution;

namespace Cratis.Arc.Queries.for_ObservableQueryHttp;

public class when_creating_http_response_for_non_observable_data : Specification
{
    QueryContext _queryContext;
    object _data;
    ObservableQueryHttpResponse _response;

    void Establish()
    {
        _queryContext = new("TestQuery", CorrelationId.New(), Paging.NotPaged, Sorting.None);
        _data = new TestData("plain");
    }

    async Task Because() => _response = await ObservableQueryHttp.CreateResponse(
        _queryContext,
        _data,
        new ObservableQueryHttpOptions(true, TimeSpan.FromSeconds(1)),
        CancellationToken.None);

    [Fact] void should_return_ok_status_code() => _response.StatusCode.ShouldEqual(HttpStatusCode.OK);
    [Fact] void should_return_the_data_as_is() => _response.Result.Data.ShouldEqual(_data);
    [Fact] void should_be_authorized() => _response.Result.IsAuthorized.ShouldBeTrue();

    record TestData(string Value);
}
