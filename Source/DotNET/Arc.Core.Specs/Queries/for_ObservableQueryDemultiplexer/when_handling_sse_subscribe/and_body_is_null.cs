// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.when_handling_sse_subscribe;

public class and_body_is_null : given.an_observable_query_demultiplexer
{
    IHttpRequestContext _context;
    int _statusCode;

    void Establish()
    {
        _context = Substitute.For<IHttpRequestContext>();
        _context.RequestAborted.Returns(CancellationToken.None);
        _context.ReadBodyAsJson(typeof(ObservableQuerySSESubscribeRequest), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<object?>(null));
        _context.When(_ => _.SetStatusCode(Arg.Any<int>()))
            .Do(ci => _statusCode = ci.Arg<int>());
    }

    async Task Because() => await _hub.HandleSSESubscribe(_context);

    [Fact] void should_return_400() => _statusCode.ShouldEqual(400);
}
