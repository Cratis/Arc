// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Execution;

namespace Cratis.Arc.Queries.for_QueryEndpointMapper.when_handling_a_query;

public class via_get : given.a_query_request
{
    void Establish() =>
        _queryPipeline.Perform(Arg.Any<FullyQualifiedQueryName>(), Arg.Any<QueryArguments>(), Arg.Any<Paging>(), Arg.Any<Sorting>(), Arg.Any<IServiceProvider>())
            .Returns(QueryResult.Success(CorrelationId.New()));

    async Task Because() => await _mapper.HandlerFor("GET")(_context);

    [Fact] void should_perform_the_query() => _queryPipeline.Received(1).Perform(Arg.Any<FullyQualifiedQueryName>(), Arg.Any<QueryArguments>(), Arg.Any<Paging>(), Arg.Any<Sorting>(), Arg.Any<IServiceProvider>());
    [Fact] void should_respond_with_ok() => _statusCode.ShouldEqual(200);
    [Fact] void should_write_the_query_result() => _context.Received(1).WriteResponseAsJson(Arg.Any<object?>(), typeof(QueryResult), Arg.Any<CancellationToken>());
    [Fact] void should_not_mark_the_response_no_store() => _context.DidNotReceive().SetResponseHeader("Cache-Control", "no-store");
}
