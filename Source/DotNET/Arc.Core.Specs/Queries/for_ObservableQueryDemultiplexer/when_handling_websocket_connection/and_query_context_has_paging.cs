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

public class and_query_context_has_paging : given.an_observable_query_demultiplexer
{
    const string ControllerQueryName = "Cratis.Chronicle.Api.EventStores.EventStoreQueries.AllEventStores";
    const string QueryId = "query-1";
    const int Page = 2;
    const int PageSize = 20;
    const int TotalItems = 137;

    IHttpRequestContext _context;
    IWebSocketContext _webSocketContext;
    IWebSocket _webSocket;
    BehaviorSubject<IEnumerable<string>> _subject;
    ConcurrentQueue<ObservableQueryHubMessage> _sentMessages;
    QueryContext _queryContextWithPaging;
    int _receiveCount;

    void Establish()
    {
        _receiveCount = 0;
        _sentMessages = [];

        _queryContextWithPaging = new QueryContext(
            new FullyQualifiedQueryName(ControllerQueryName),
            CorrelationId.New(),
            new Paging(Page, PageSize, true),
            Sorting.None)
        {
            TotalItems = TotalItems
        };

        _queryContextManager.Current.Returns(_queryContextWithPaging);

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

    async Task Because() => await _hub.HandleWebSocketConnection(_context);

    [Fact]
    void should_send_paging_page_from_query_context() =>
        GetPagingFromResultMessage().GetProperty("page").GetInt32().ShouldEqual(Page);

    [Fact]
    void should_send_paging_size_from_query_context() =>
        GetPagingFromResultMessage().GetProperty("size").GetInt32().ShouldEqual(PageSize);

    [Fact]
    void should_send_paging_total_items_from_query_context() =>
        GetPagingFromResultMessage().GetProperty("totalItems").GetInt32().ShouldEqual(TotalItems);

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
                {
                    Page = Page,
                    PageSize = PageSize
                }
            };

            var bytes = JsonSerializer.SerializeToUtf8Bytes(subscribeMessage, _arcOptions.Value.JsonSerializerOptions);

            // Emit on a thread that, by the time the callback runs, no longer has the request-scoped
            // QueryContext available — simulating the MongoDB change stream callback. The fix must
            // capture the context at subscribe time; reading it at callback time would observe
            // QueryContext.NotSet and overwrite the paging information.
            _ = Task.Factory.StartNew(
                () =>
                {
                    Thread.Sleep(50);
                    _queryContextManager.Current.Returns(QueryContext.NotSet);
                    _subject.OnNext(["event-store-a"]);
                },
                CancellationToken.None,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);

            return CopyToReceiveBuffer(bytes, buffer);
        }

        await Task.Delay(200);
        return new WebSocketReceiveResult(0, System.Net.WebSockets.WebSocketMessageType.Close, true, WebSocketCloseStatus.NormalClosure, "done");
    }

    static WebSocketReceiveResult CopyToReceiveBuffer(byte[] data, ArraySegment<byte> buffer)
    {
        Array.Copy(data, 0, buffer.Array, buffer.Offset, data.Length);
        return new WebSocketReceiveResult(data.Length, System.Net.WebSockets.WebSocketMessageType.Text, true);
    }

    JsonElement GetPagingFromResultMessage()
    {
        // The BehaviorSubject emits its initial empty value synchronously during Subscribe,
        // when the mock still returns the populated query context. Inspect the emission that
        // happened *after* the mock was swapped to NotSet — that is the one a real MongoDB
        // change-stream callback would represent. Identify it by the non-empty data payload.
        foreach (var hubMessage in _sentMessages)
        {
            if (hubMessage.Type != ObservableQueryHubMessageType.QueryResult ||
                hubMessage.QueryId != QueryId ||
                hubMessage.Payload is not JsonElement payload ||
                payload.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            if (!TryGetPropertyIgnoreCase(payload, "data", out var data) ||
                data.ValueKind != JsonValueKind.Array ||
                data.GetArrayLength() == 0)
            {
                continue;
            }

            if (TryGetPropertyIgnoreCase(payload, "paging", out var paging))
            {
                return paging;
            }
        }

        throw new InvalidOperationException("No QueryResult message with data and paging was sent.");
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
