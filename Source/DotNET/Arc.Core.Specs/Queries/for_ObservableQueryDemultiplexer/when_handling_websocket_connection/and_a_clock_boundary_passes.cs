// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Reactive.Subjects;
using System.Text;
using System.Text.Json;
using Cratis.Arc.Calendar;
using Cratis.Arc.Http;
using Cratis.Arc.Tenancy;
using Cratis.Execution;
using Microsoft.Extensions.Time.Testing;

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.when_handling_websocket_connection;

public class and_a_clock_boundary_passes : given.an_observable_query_demultiplexer
{
    const string ControllerQueryName = "Cratis.Chronicle.Api.EventStores.EventStoreQueries.AllEventStores";
    const string QueryId = "query-1";

    IHttpRequestContext _context;
    IWebSocketContext _webSocketContext;
    IWebSocket _webSocket;
    static readonly DateTimeOffset _start = new(2026, 5, 1, 12, 0, 0, TimeSpan.Zero);
    static readonly DateOnly _dueDate = new(2026, 5, 2);

    FakeTimeProvider _time;
    ISubject<IEnumerable<string>> _subject;
    ConcurrentQueue<ObservableQueryHubMessage> _sentMessages;
    int _receiveCount;
    readonly TaskCompletionSource _initialResultSent = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _dataResultSent = new(TaskCreationOptions.RunContinuationsAsynchronously);
    Task _producerTask = Task.CompletedTask;

    void Establish()
    {
        _receiveCount = 0;
        _sentMessages = [];

        _time = new FakeTimeProvider(_start);
        var context = new ClockBoundaryContext(
            new TenantId("tenant"),
            TimeZoneInfo.Utc,
            _time,
            Substitute.For<ITenantScope>(),
            new ChangeSetComputor(_arcOptions.Value.JsonSerializerOptions),
            CancellationToken.None);
        _subject = new BehaviorSubject<string>("event-store-a").ReevaluateAtBoundaries(
            context,
            (item, reading) => new ClockEvaluation<IEnumerable<string>>(
                reading.Today >= _dueDate ? [item] : [],
                LocalDateChanges.NextLocalDateChange(reading.Zone, reading.Instant)));
        _queryPipeline.Perform(Arg.Any<FullyQualifiedQueryName>(), Arg.Any<QueryArguments>(), Arg.Any<Paging>(), Arg.Any<Sorting>(), Arg.Any<IServiceProvider>(), Arg.Any<CancellationToken>())
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
                    if (hubMessage.Type == ObservableQueryHubMessageType.QueryResult &&
                        hubMessage.QueryId == QueryId &&
                        hubMessage.Payload is JsonElement payload &&
                        TryGetPropertyIgnoreCase(payload, "data", out var resultData) &&
                        resultData.ValueKind == JsonValueKind.Array)
                    {
                        if (resultData.GetArrayLength() == 0)
                        {
                            _initialResultSent.TrySetResult();
                        }
                        else
                        {
                            _dataResultSent.TrySetResult();
                        }
                    }

                    _signals.Signal();
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
            Arg.Any<IServiceProvider>(),
            Arg.Any<CancellationToken>());

    [Fact] void should_send_the_clock_epoch_as_a_second_query_result_over_websocket() => _sentMessages.Count(_ => _.Type == ObservableQueryHubMessageType.QueryResult && _.QueryId == QueryId).ShouldEqual(2);
    [Fact] void should_send_non_empty_data_result_over_websocket() => HasDataQueryResultMessage().ShouldBeTrue();

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

            _producerTask = ProduceDataAfterInitialResult();

            return CopyToReceiveBuffer(bytes, buffer);
        }

        await _producerTask;
        await WaitFor(() => _dataResultSent.Task.IsCompleted);
        return new WebSocketReceiveResult(0, System.Net.WebSockets.WebSocketMessageType.Close, true, WebSocketCloseStatus.NormalClosure, "done");
    }

    async Task ProduceDataAfterInitialResult()
    {
        await WaitFor(() => _initialResultSent.Task.IsCompleted);
        _time.Advance(TimeSpan.FromHours(13));
    }

    static WebSocketReceiveResult CopyToReceiveBuffer(byte[] data, ArraySegment<byte> buffer)
    {
        Array.Copy(data, 0, buffer.Array, buffer.Offset, data.Length);
        return new WebSocketReceiveResult(data.Length, System.Net.WebSockets.WebSocketMessageType.Text, true);
    }

    bool HasDataQueryResultMessage() => _sentMessages.Any(hubMessage =>
        hubMessage.Type == ObservableQueryHubMessageType.QueryResult &&
        hubMessage.QueryId == QueryId &&
        hubMessage.Payload is JsonElement payload &&
        TryGetPropertyIgnoreCase(payload, "data", out var data) &&
        data.ValueKind == JsonValueKind.Array &&
        data.EnumerateArray().Any(item => item.ValueKind == JsonValueKind.String && item.GetString() == "event-store-a"));

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