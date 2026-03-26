// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.when_handling_sse_unsubscribe;

public class and_connection_id_is_unknown : given.an_observable_query_demultiplexer
{
    IHttpRequestContext _context;
    int _statusCode;

    void Establish()
    {
        _context = Substitute.For<IHttpRequestContext>();
        _context.RequestAborted.Returns(CancellationToken.None);

        var body = new ObservableQuerySSEUnsubscribeRequest(
            "non-existent-connection-id",
            "query-1");

        _context.ReadBodyAsJson(typeof(ObservableQuerySSEUnsubscribeRequest), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<object?>(body));
        _context.When(_ => _.SetStatusCode(Arg.Any<int>()))
            .Do(ci => _statusCode = ci.Arg<int>());
    }

    async Task Because() => await _hub.HandleSSEUnsubscribe(_context);

    [Fact] void should_return_404() => _statusCode.ShouldEqual(404);
}
