// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Reactive.Subjects;
using Cratis.Execution;

namespace Cratis.Arc.Queries.for_ObservableQueryHttp;

public class when_waiting_for_first_result : Specification
{
    QueryContext _queryContext;
    Subject<TestData> _subject;
    ObservableQueryHttpResponse _response;

    void Establish()
    {
        _queryContext = new("TestQuery", CorrelationId.New(), Paging.NotPaged, Sorting.None);
        _subject = new Subject<TestData>();
    }

    async Task Because()
    {
        var responseTask = ObservableQueryHttp.CreateResponse(
            _queryContext,
            _subject,
            new ObservableQueryHttpOptions(true, TimeSpan.FromSeconds(1)),
            CancellationToken.None);

        await Task.Delay(50);
        _subject.OnNext(new("first"));

        _response = await responseTask;
    }

    [Fact] void should_return_ok_status_code() => _response.StatusCode.ShouldEqual(HttpStatusCode.OK);
    [Fact] void should_return_first_result() => ((TestData)_response.Result.Data).Value.ShouldEqual("first");

    record TestData(string Value);
}
