// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Cratis.Arc.Http;

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.given;

/// <summary>
/// Drives a full WebSocket connection: every query id in <see cref="a_guarded_connection._queryIds"/> is subscribed in
/// turn, the spec's script then runs against the live subscriptions, and the client closes.
/// </summary>
public class a_guarded_websocket_connection : a_guarded_connection
{
    protected IHttpRequestContext _context;
    protected IWebSocket _webSocket;
    protected ConcurrentQueue<ObservableQueryHubMessage> _sentMessages;

    Func<Task> _script = () => Task.CompletedTask;
    int _receiveCount;

    void Establish()
    {
        _sentMessages = [];
        _receiveCount = 0;

        _context = Substitute.For<IHttpRequestContext>();
        _context.RequestAborted.Returns(CancellationToken.None);
        _context.RequestServices.Returns(Substitute.For<IServiceProvider>());
        _context.User.Returns(_principal);

        var webSocketContext = Substitute.For<IWebSocketContext>();
        _context.WebSockets.Returns(webSocketContext);

        _webSocket = Substitute.For<IWebSocket>();
        _webSocket.State.Returns(WebSocketState.Open);
        webSocketContext.AcceptWebSocket(Arg.Any<CancellationToken>()).Returns(Task.FromResult(_webSocket));

        _webSocket.Receive(Arg.Any<ArraySegment<byte>>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => ReceiveNextMessage(callInfo.Arg<ArraySegment<byte>>()));

        _webSocket.Send(Arg.Any<ArraySegment<byte>>(), Arg.Any<System.Net.WebSockets.WebSocketMessageType>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var data = callInfo.Arg<ArraySegment<byte>>();
                var json = Encoding.UTF8.GetString(data.Array, data.Offset, data.Count);
                var hubMessage = JsonSerializer.Deserialize<ObservableQueryHubMessage>(json, _arcOptions.Value.JsonSerializerOptions);
                if (hubMessage is not null)
                {
                    _sentMessages.Enqueue(hubMessage);
                }

                return Task.CompletedTask;
            });

        _webSocket.Close(Arg.Any<WebSocketCloseStatus>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
    }

    protected async Task RunConnection(Func<Task> script)
    {
        _script = script;
        await _hub.HandleWebSocketConnection(_context);
    }

    protected int CountQueryResultsFor(string queryId) =>
        _sentMessages.Count(_ => _.Type == ObservableQueryHubMessageType.QueryResult && _.QueryId == queryId);

    protected bool HasUnauthorizedFor(string queryId) =>
        _sentMessages.Any(_ => _.Type == ObservableQueryHubMessageType.Unauthorized && _.QueryId == queryId);

    protected bool HasErrorFor(string queryId) =>
        _sentMessages.Any(_ => _.Type == ObservableQueryHubMessageType.Error && _.QueryId == queryId);

    protected IEnumerable<object> DataOfQueryResultsFor(string queryId) =>
        _sentMessages
            .Where(_ => _.Type == ObservableQueryHubMessageType.QueryResult && _.QueryId == queryId)
            .Select(_ => _.Payload!);

    async Task<WebSocketReceiveResult> ReceiveNextMessage(ArraySegment<byte> buffer)
    {
        if (_receiveCount < _queryIds.Length)
        {
            var subscribe = new ObservableQueryHubMessage
            {
                Type = ObservableQueryHubMessageType.Subscribe,
                QueryId = _queryIds[_receiveCount++],
                Payload = new ObservableQuerySubscriptionRequest(QueryName, RawArguments)
            };

            var bytes = JsonSerializer.SerializeToUtf8Bytes(subscribe, _arcOptions.Value.JsonSerializerOptions);
            Array.Copy(bytes, 0, buffer.Array!, buffer.Offset, bytes.Length);
            return new WebSocketReceiveResult(bytes.Length, System.Net.WebSockets.WebSocketMessageType.Text, true);
        }

        if (_receiveCount == _queryIds.Length)
        {
            _receiveCount++;
            await _script();
        }

        return new WebSocketReceiveResult(0, System.Net.WebSockets.WebSocketMessageType.Close, true, WebSocketCloseStatus.NormalClosure, "done");
    }
}
