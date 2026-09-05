// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_QueryEndpointMapper.when_handling_a_query;

public class via_query_with_an_unreadable_body : given.a_query_request
{
    void Establish() =>
        _context.ReadBodyAsJson(typeof(QueryRequestEnvelope), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<object?>(new InvalidOperationException("missing content type")));

    async Task Because() => await _mapper.HandlerFor("QUERY")(_context);

    [Fact] void should_mark_the_response_no_store() => _context.Received(1).SetResponseHeader("Cache-Control", "no-store");
    [Fact] void should_respond_with_bad_request() => _statusCode.ShouldEqual(400);
    [Fact] void should_write_an_error_result() => _context.Received(1).WriteResponseAsJson(Arg.Any<object?>(), typeof(QueryResult), Arg.Any<CancellationToken>());
    [Fact] void should_not_perform_the_query() => _queryPipeline.DidNotReceive().Perform(Arg.Any<FullyQualifiedQueryName>(), Arg.Any<QueryArguments>(), Arg.Any<Paging>(), Arg.Any<Sorting>(), Arg.Any<IServiceProvider>());
}
