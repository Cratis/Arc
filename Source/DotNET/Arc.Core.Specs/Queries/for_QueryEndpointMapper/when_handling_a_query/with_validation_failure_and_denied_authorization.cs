// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_QueryEndpointMapper.when_handling_a_query;

public class with_validation_failure_and_denied_authorization : given.a_query_rejected_by_validation
{
    void Establish()
    {
        _rejection.IsAuthorized = false;
        _context.WebSockets.IsWebSocketRequest.Returns(true);
        _context.Headers.Returns(new Dictionary<string, string> { ["Accept"] = "text/event-stream" });
    }

    Task Because() => _mapper.HandlerFor("GET")(_context);

    [Fact] void should_return_forbidden() => _statusCode.ShouldEqual(403);
    [Fact] void should_not_upgrade_the_websocket() => _context.WebSockets.DidNotReceive().AcceptWebSocket(Arg.Any<CancellationToken>());
    [Fact] void should_not_start_an_sse_stream() => _context.DidNotReceive().Write(Arg.Any<string>(), Arg.Any<CancellationToken>());
}
