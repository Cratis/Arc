// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Reactive.Subjects;
using System.Text.Json;
using Cratis.Arc.Http;
using Cratis.Execution;

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.when_handling_websocket_connection;

/// <summary>
/// Reproduces the production-crashing race on the WebSocket path where an observable-query emission is in
/// flight when the connection ends and its per-connection resources — the write lock (a
/// <see cref="SemaphoreSlim"/>), the emission gate and the linked <see cref="CancellationTokenSource"/> — are
/// disposed. When the emission resumes it touches those disposed objects; because the emission callback is
/// <c>async void</c>, the resulting <see cref="ObjectDisposedException"/> would previously terminate the whole
/// process. The emission must instead be handled gracefully.
/// </summary>
public class and_emission_arrives_after_connection_is_disposed : given.an_observable_query_demultiplexer
{
    const string ControllerQueryName = "Cratis.Chronicle.Api.EventStores.EventStoreQueries.AllEventStores";
    const string QueryId = "query-1";

    IQueryHealthTracker _observableHealthTracker;
    IHttpRequestContext _context;
    IWebSocketContext _webSocketContext;
    IWebSocket _webSocket;
    BehaviorSubject<IEnumerable<string>> _subject;
    ConcurrentQueue<ObservableQueryHubMessage> _sentMessages;
    TaskCompletionSource<IEnumerable<object>> _interceptionGate;
    TaskCompletionSource _dataServed;
    int _receiveCount;
    bool _closeRequested;
    bool _connectionCompleted;
    Exception _thrownWhenReleasingInFlightEmission;

    void Establish()
    {
        _receiveCount = 0;
        _sentMessages = [];
        _interceptionGate = new TaskCompletionSource<IEnumerable<object>>();
        _dataServed = new TaskCompletionSource();

        // Rebuild the hub with a health tracker we can observe: RecordDataServed is invoked by the emission
        // path only after the transport send returns, so its call is the signal that the in-flight emission
        // ran to completion gracefully instead of crashing.
        _observableHealthTracker = Substitute.For<IQueryHealthTracker>();
        _observableHealthTracker
            .When(_ => _.RecordDataServed(Arg.Any<string>(), Arg.Any<string>()))
            .Do(_ => _dataServed.TrySetResult());

        _hub = new ObservableQueryDemultiplexer(
            _queryPipeline,
            _queryContextManager,
            _httpRequestContextAccessor,
            _hostApplicationLifetime,
            _readModelInterceptors,
            _serviceProvider,
            _arcOptions,
            _observableHealthTracker,
            _logger);

        // Hold interception open so the first emission stays in flight (holding the emission gate) while the
        // connection is torn down underneath it.
        _readModelInterceptors.Intercept(Arg.Any<Type>(), Arg.Any<IEnumerable<object>>(), Arg.Any<IServiceProvider>())
            .Returns(_ => _interceptionGate.Task);

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
                var json = System.Text.Encoding.UTF8.GetString(data.Array!, data.Offset, data.Count);
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

    async Task Because()
    {
        var connectionTask = _hub.HandleWebSocketConnection(_context);

        // Subscribing to the BehaviorSubject immediately delivers its current value, so an emission is now in
        // flight and suspended inside interception, holding the emission gate.
        await WaitFor(() => _readModelInterceptors.ReceivedCalls().Any());

        // Close the socket. The finally disposes the subscription (emission gate), the write lock and the
        // linked cancellation source — all while the emission is still in flight.
        _closeRequested = true;
        await connectionTask;
        _connectionCompleted = true;

        // Let the in-flight emission resume against the now-disposed resources. Before the fix this crashed
        // the process; it must now be a graceful no-op.
        try
        {
            _interceptionGate.SetResult(["item-a"]);
            await WaitFor(() => _dataServed.Task.IsCompleted);
        }
        catch (Exception ex)
        {
            _thrownWhenReleasingInFlightEmission = ex;
        }
    }

    [Fact] void should_complete_the_connection_cleanly() => _connectionCompleted.ShouldBeTrue();
    [Fact] void should_not_throw_when_the_in_flight_emission_resumes() => _thrownWhenReleasingInFlightEmission.ShouldBeNull();
    [Fact] void should_handle_the_late_emission_gracefully() => _dataServed.Task.IsCompleted.ShouldBeTrue();
    [Fact] void should_not_send_a_query_result_over_the_disposed_socket() => HasQueryResultMessage().ShouldBeFalse();

    bool HasQueryResultMessage() =>
        _sentMessages.Any(_ => _.Type == ObservableQueryHubMessageType.QueryResult && _.QueryId == QueryId);

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
            Array.Copy(bytes, 0, buffer.Array!, buffer.Offset, bytes.Length);
            return new WebSocketReceiveResult(bytes.Length, System.Net.WebSockets.WebSocketMessageType.Text, true);
        }

        // Keep the connection open until the spec asks it to close, then signal a normal close so the read
        // loop exits and the connection is torn down.
        while (!_closeRequested)
        {
            await Task.Delay(10);
        }

        return new WebSocketReceiveResult(0, System.Net.WebSockets.WebSocketMessageType.Close, true, WebSocketCloseStatus.NormalClosure, "done");
    }
}
