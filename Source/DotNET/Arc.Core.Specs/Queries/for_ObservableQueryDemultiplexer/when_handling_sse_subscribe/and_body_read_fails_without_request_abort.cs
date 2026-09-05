// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.when_handling_sse_subscribe;

public class and_body_read_fails_without_request_abort : given.an_observable_query_demultiplexer
{
    IHttpRequestContext _context;
    Exception? _error;

    void Establish()
    {
        _context = Substitute.For<IHttpRequestContext>();
        _context.RequestAborted.Returns(CancellationToken.None);
        _context.ReadBodyAsJson(typeof(ObservableQuerySSESubscribeRequest), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<object?>(new IOException("The request body ended unexpectedly.")));
    }

    async Task Because() => _error = await Catch.Exception(() => _hub.HandleSSESubscribe(_context));

    [Fact] void should_propagate_the_body_read_error() => _error.ShouldBeOfExactType<IOException>();
    [Fact] void should_not_set_a_status_code() => _context.DidNotReceiveWithAnyArgs().SetStatusCode(default);

    [Fact]
    void should_not_perform_a_query() =>
        _queryPipeline.DidNotReceiveWithAnyArgs().Perform(
            default!,
            default!,
            default!,
            default!,
            default!);
}
