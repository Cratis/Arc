// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Cratis.Arc.Http;
using SocketMessageType = System.Net.WebSockets.WebSocketMessageType;

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.when_handling_websocket_connection;

public class and_pipeline_returns_live_paging_context : given.paged_multiplexed_subscriptions
{
    IHttpRequestContext _context;
    IWebSocket _socket;
    ConcurrentQueue<ObservableQueryHubMessage> _messages;
    int _receiveCount;

    void Establish()
    {
        _messages = [];
        _context = Substitute.For<IHttpRequestContext>();
        _context.RequestAborted.Returns(CancellationToken.None);
        _context.RequestServices.Returns(Substitute.For<IServiceProvider>());
        _socket = Substitute.For<IWebSocket>();
        _socket.State.Returns(WebSocketState.Open);
        _context.WebSockets.AcceptWebSocket(Arg.Any<CancellationToken>()).Returns(Task.FromResult(_socket));
        _socket.Receive(Arg.Any<ArraySegment<byte>>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => Receive(callInfo.Arg<ArraySegment<byte>>()));
        _socket.Send(Arg.Any<ArraySegment<byte>>(), Arg.Any<SocketMessageType>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var data = callInfo.Arg<ArraySegment<byte>>();
                var message = JsonSerializer.Deserialize<ObservableQueryHubMessage>(Encoding.UTF8.GetString(data.Array!, data.Offset, data.Count), _arcOptions.Value.JsonSerializerOptions);
                if (message is not null)
                {
                    _messages.Enqueue(message);
                    _signals.Signal();
                }
                return Task.CompletedTask;
            });
        _socket.Close(Arg.Any<WebSocketCloseStatus>(), Arg.Any<string?>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
    }

    async Task Because() => await _hub.HandleWebSocketConnection(_context);

    [Fact] void should_start_with_an_empty_result() => Property(PayloadFor(_messages, FirstId, 0), "data").GetArrayLength().ShouldEqual(0);
    [Fact] void should_include_paging_on_first_empty_emission() => AssertPaging(PagingFor(_messages, FirstId, 0), 137);
    [Fact] void should_include_updated_total_items_on_later_emission() => AssertPaging(PagingFor(_messages, FirstId, 1), 141);
    [Fact] void should_keep_the_second_subscription_independent() => AssertPaging(PagingFor(_messages, SecondId, 1), 23);
    [Fact] void should_not_depend_on_ambient_context() => _queryContextManager.Current.ShouldEqual(QueryContext.NotSet);

    async Task<WebSocketReceiveResult> Receive(ArraySegment<byte> buffer)
    {
        if (_receiveCount < 2)
        {
            var first = _receiveCount++ == 0;
            var request = new ObservableQueryHubMessage
            {
                Type = ObservableQueryHubMessageType.Subscribe,
                QueryId = first ? FirstId : SecondId,
                Payload = new ObservableQuerySubscriptionRequest(first ? FirstQuery : SecondQuery) { Page = Page, PageSize = Size }
            };
            var bytes = JsonSerializer.SerializeToUtf8Bytes(request, _arcOptions.Value.JsonSerializerOptions);
            Array.Copy(bytes, 0, buffer.Array!, buffer.Offset, bytes.Length);
            return new WebSocketReceiveResult(bytes.Length, SocketMessageType.Text, true);
        }

        await WaitFor(() => ResultCount(FirstId) == 1 && ResultCount(SecondId) == 1);
        FirstContext.TotalItems = 141;
        FirstSubject.OnNext(["first-item"]);
        await WaitFor(() => ResultCount(FirstId) == 2);
        SecondSubject.OnNext(["second-item"]);
        await WaitFor(() => ResultCount(SecondId) == 2);
        return new WebSocketReceiveResult(0, SocketMessageType.Close, true, WebSocketCloseStatus.NormalClosure, "done");
    }

    int ResultCount(string queryId) => _messages.Count(message => message.Type == ObservableQueryHubMessageType.QueryResult && message.QueryId == queryId);
}
