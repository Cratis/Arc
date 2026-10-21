// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.WebSockets;
using System.Text.Json;
using Cratis.Arc.Http;

namespace Cratis.Arc.Queries.for_QueryEndpointMapper.when_handling_a_query;

public class with_validation_failure_over_websocket : given.a_query_rejected_by_validation
{
    IWebSocket _socket;
    QueryResult _received;

    void Establish()
    {
        _socket = Substitute.For<IWebSocket>();
        _context.WebSockets.IsWebSocketRequest.Returns(true);
        _context.WebSockets.AcceptWebSocket(Arg.Any<CancellationToken>()).Returns(_socket);
        _socket.Send(Arg.Any<ArraySegment<byte>>(), System.Net.WebSockets.WebSocketMessageType.Text, true, Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                _received = JsonSerializer.Deserialize<QueryResult>(call.Arg<ArraySegment<byte>>().AsSpan(), _arcOptions.JsonSerializerOptions)!;
                return Task.CompletedTask;
            });
    }

    Task Because() => _mapper.HandlerFor("GET")(_context);

    [Fact] void should_accept_the_websocket() => _context.WebSockets.Received(1).AcceptWebSocket(Arg.Any<CancellationToken>());
    [Fact] void should_send_one_frame() => _socket.Received(1).Send(Arg.Any<ArraySegment<byte>>(), System.Net.WebSockets.WebSocketMessageType.Text, true, Arg.Any<CancellationToken>());
    [Fact] void should_be_invalid() => _received.IsValid.ShouldBeFalse();
    [Fact] void should_not_be_successful() => _received.IsSuccess.ShouldBeFalse();
    [Fact] void should_preserve_message() => _received.ValidationResults.Single().Message.ShouldEqual("Search term is required.");
    [Fact] void should_preserve_members() => _received.ValidationResults.Single().Members.ShouldContainOnly("searchTerm");
    [Fact] void should_preserve_correlation_id() => _received.CorrelationId.ShouldEqual(_rejection.CorrelationId);
    [Fact] void should_not_send_data() => _received.Data.ShouldBeNull();
    [Fact] void should_close_the_socket() => _socket.Received(1).Close(WebSocketCloseStatus.NormalClosure, Arg.Any<string>(), Arg.Any<CancellationToken>());
    [Fact] void should_not_send_an_http_body() => _context.DidNotReceive().WriteResponseAsJson(Arg.Any<object>(), Arg.Any<Type>(), Arg.Any<CancellationToken>());
}
