// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Reactive.Subjects;
using System.Text;
using System.Text.Json;
using Cratis.Arc.Http;
using Cratis.Execution;

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.when_handling_websocket_connection;

public class and_query_streams_results : given.an_observable_query_demultiplexer
{
    const string ControllerQueryName = "Cratis.Chronicle.Api.EventStores.EventStoreQueries.AllEventStores";
    const string QueryId = "query-1";

    IHttpRequestContext _context;
    IWebSocketContext _webSocketContext;
    IWebSocket _webSocket;
    BehaviorSubject<IEnumerable<string>> _subject;
    ConcurrentQueue<ObservableQueryHubMessage> _sentMessages;
    int _receiveCount;

    void Establish()
    {
        _receiveCount = 0;
        _sentMessages = [];

        _subject = new BehaviorSubject<IEnumerable<string>>([]);
        _queryPipeline.Perform(
                Arg.Any<FullyQualifiedQueryName>(),
                Arg.Any<QueryArguments>(),
                Arg.Any<Paging>(),
                Arg.Any<Sorting>(),
                Arg.Any<IServiceProvider>())
            .Returns(_ =>
            {
                var queryResult = QueryResult.Success(CorrelationId.New());
                queryResult.Data = _subject;
                return Task.FromResult(queryResult);
            });

        _context = Substitute.For<IHttpRequestContext>();
        _context.RequestAborted.Returns(CancellationToken.None);
        _context.RequestServices.Returns(Substitute.For<IServiceProvider>());

        _webSocketContext = Substitute.For<IWebSocketContext>();
        _context.WebSockets.Returns(_webSocketContext);

        _webSocket = Substitute.For<IWebSocket>();
        _webSocket.State.Returns(WebSocketState.Open);
        _webSocketContext.AcceptWebSocket(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(_webSocket));

        _webSocket.Receive(Arg.Any<ArraySegment<byte>>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => ReceiveNextMessage(callInfo.Arg<ArraySegment<byte>>()));

        _webSocket.Send(Arg.Any<ArraySegment<byte>>(), Arg.Any<System.Net.WebSockets.WebSocketMessageType>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var data = callInfo.Arg<ArraySegment<byte>>();
                var json = Encoding.UTF8.GetString(data.Array!, data.Offset, data.Count);
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

    async Task Because() => await _hub.HandleWebSocketConnection(_context);

    [Fact]
    void should_call_query_pipeline_for_controller_fully_qualified_name() =>
        _queryPipeline.Received(1).Perform(
            Arg.Is<FullyQualifiedQueryName>(_ => _.Value == ControllerQueryName),
            Arg.Any<QueryArguments>(),
            Arg.Any<Paging>(),
            Arg.Any<Sorting>(),
            Arg.Any<IServiceProvider>());

    [Fact] void should_send_query_result_message_over_websocket() => HasQueryResultMessage().ShouldBeTrue();

    async Task<WebSocketReceiveResult> ReceiveNextMessage(ArraySegment<byte> buffer)
    {
        if (_receiveCount == 0)
        {
            _receiveCount++;

            var subscribeMessage = new ObservableQueryHubMessage
            {
                Type = ObservableQueryHubMessageType.Subscribe,
                QueryId = QueryId,
                Payload = new ObservableQuerySubscriptionRequest(ControllerQueryName)
            };

            var bytes = JsonSerializer.SerializeToUtf8Bytes(subscribeMessage, _arcOptions.Value.JsonSerializerOptions);

            _ = Task.Run(async () =>
            {
                await Task.Delay(50);
                _subject.OnNext(["event-store-a"]);
            });

            return CopyToReceiveBuffer(bytes, buffer);
        }

        await Task.Delay(150);
        return new WebSocketReceiveResult(0, System.Net.WebSockets.WebSocketMessageType.Close, true, WebSocketCloseStatus.NormalClosure, "done");
    }

    static WebSocketReceiveResult CopyToReceiveBuffer(byte[] data, ArraySegment<byte> buffer)
    {
        Array.Copy(data, 0, buffer.Array!, buffer.Offset, data.Length);
        return new WebSocketReceiveResult(data.Length, System.Net.WebSockets.WebSocketMessageType.Text, true);
    }

    bool HasQueryResultMessage()
    {
        foreach (var hubMessage in _sentMessages)
        {
            if (hubMessage.Type != ObservableQueryHubMessageType.QueryResult ||
                hubMessage.QueryId != QueryId ||
                hubMessage.Payload is not JsonElement payload ||
                payload.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            return TryGetPropertyIgnoreCase(payload, "data", out _);
        }

        return false;
    }

    static bool TryGetPropertyIgnoreCase(JsonElement element, string propertyName, out JsonElement value)
    {
        if (element.TryGetProperty(propertyName, out value))
        {
            return true;
        }

        var alternateCasing = char.ToUpperInvariant(propertyName[0]) + propertyName[1..];
        return element.TryGetProperty(alternateCasing, out value);
    }
}