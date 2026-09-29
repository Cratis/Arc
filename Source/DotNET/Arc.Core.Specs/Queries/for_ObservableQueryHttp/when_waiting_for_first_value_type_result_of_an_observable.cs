// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Reactive.Linq;
using Cratis.Execution;

namespace Cratis.Arc.Queries.for_ObservableQueryHttp;

/// <summary>
/// A plain <see cref="IObservable{T}"/> that is not a subject has no current value to read, so the response waits for
/// its first emission.
/// </summary>
public class when_waiting_for_first_value_type_result_of_an_observable : Specification
{
    QueryContext _queryContext;
    ObservableQueryHttpResponse _response;

    void Establish() => _queryContext = new("TestQuery", CorrelationId.New(), Paging.NotPaged, Sorting.None);

    async Task Because() => _response = await ObservableQueryHttp.CreateResponse(
        _queryContext,
        Observable.Return(7),
        new ObservableQueryHttpOptions(true, TimeSpan.FromSeconds(1)),
        CancellationToken.None);

    [Fact] void should_return_ok_status_code() => _response.StatusCode.ShouldEqual(HttpStatusCode.OK);
    [Fact] void should_return_the_first_result() => _response.Result.Data.ShouldEqual(7);
}
