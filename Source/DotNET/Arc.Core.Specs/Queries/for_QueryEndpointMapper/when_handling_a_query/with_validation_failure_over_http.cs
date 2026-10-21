// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_QueryEndpointMapper.when_handling_a_query;

public class with_validation_failure_over_http : given.a_query_rejected_by_validation
{
    Task Because() => _mapper.HandlerFor("GET")(_context);

    [Fact] void should_return_bad_request() => _statusCode.ShouldEqual(400);
    [Fact] void should_send_the_validation_result() => _context.Received(1).WriteResponseAsJson(_rejection, typeof(QueryResult), Arg.Any<CancellationToken>());
    [Fact] void should_not_accept_a_websocket() => _context.WebSockets.DidNotReceive().AcceptWebSocket(Arg.Any<CancellationToken>());
}
