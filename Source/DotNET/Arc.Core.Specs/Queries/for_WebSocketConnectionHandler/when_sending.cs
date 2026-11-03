// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.WebSockets;
using System.Text.Json;
using System.Text.Json.Serialization;
using Cratis.Arc.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Queries.for_WebSocketConnectionHandler;

public class when_sending : Specification
{
    readonly InvalidOperationException _error = new("Send failed");
    readonly Dictionary<string, WebSocketSendResult> _results = [];
    readonly List<int> _lockCounts = [];
    Exception? _legacySuccess;
    Exception? _legacyFailure;
    bool _waitWasPending;
    bool _transportWasPending;
    int _closedWrites;
    SemaphoreSlim _mutex;
    CancellationTokenSource _cancellation;

    async Task Because()
    {
        var handler = CreateHandler();
        var socket = Substitute.For<IWebSocket>();
        socket.State.Returns(WebSocketState.Open);
        _mutex = new SemaphoreSlim(1, 1);
        var mutex = _mutex;
        _results["sent"] = await handler.Send(socket, new QueryResult(), mutex, CancellationToken.None);
        _lockCounts.Add(mutex.CurrentCount);
        _legacySuccess = await handler.SendMessage(socket, new QueryResult(), mutex, CancellationToken.None);
        socket.Send(Arg.Any<ArraySegment<byte>>(), Arg.Any<System.Net.WebSockets.WebSocketMessageType>(), Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(_ =>
        {
            socket.State.Returns(WebSocketState.Closed);
            return Task.CompletedTask;
        });
        _results["immediateClose"] = await handler.Send(socket, new QueryResult(), mutex, CancellationToken.None);
        _lockCounts.Add(mutex.CurrentCount);

        socket.State.Returns(WebSocketState.Closed);
        socket.ClearReceivedCalls();
        _results["closed"] = await handler.Send(socket, new QueryResult(), mutex, CancellationToken.None);
        _closedWrites = socket.ReceivedCalls().Count(_ => _.GetMethodInfo().Name == nameof(IWebSocket.Send));
        _lockCounts.Add(mutex.CurrentCount);

        socket.Send(Arg.Any<ArraySegment<byte>>(), Arg.Any<System.Net.WebSockets.WebSocketMessageType>(), Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        socket.State.Returns(WebSocketState.Open);
        await mutex.WaitAsync();
        var waiting = handler.Send(socket, new QueryResult(), mutex, CancellationToken.None);
        _waitWasPending = !waiting.IsCompleted;
        socket.State.Returns(WebSocketState.Closed);
        mutex.Release();
        _results["closedAfterLock"] = await waiting.WaitAsync(TimeSpan.FromSeconds(5));
        _lockCounts.Add(mutex.CurrentCount);

        socket.State.Returns(WebSocketState.Open);
        await mutex.WaitAsync();
        _cancellation = new CancellationTokenSource();
        var cancellation = _cancellation;
        var cancelled = handler.Send(socket, new QueryResult(), mutex, cancellation.Token);
        await cancellation.CancelAsync();
        _results["cancelledWait"] = await cancelled.WaitAsync(TimeSpan.FromSeconds(5));
        mutex.CurrentCount.ShouldEqual(0);
        mutex.Release();
        _lockCounts.Add(mutex.CurrentCount);
        _results["alreadyCancelled"] = await handler.Send(socket, new QueryResult(), mutex, cancellation.Token);
        _lockCounts.Add(mutex.CurrentCount);

        var transport = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        socket.Send(Arg.Any<ArraySegment<byte>>(), Arg.Any<System.Net.WebSockets.WebSocketMessageType>(), Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(transport.Task);
        var sending = handler.Send(socket, new QueryResult(), mutex, CancellationToken.None);
        _transportWasPending = !sending.IsCompleted;
        transport.SetResult();
        _results["awaited"] = await sending;
        _lockCounts.Add(mutex.CurrentCount);

        socket.Send(Arg.Any<ArraySegment<byte>>(), Arg.Any<System.Net.WebSockets.WebSocketMessageType>(), Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(Task.FromException(new OperationCanceledException()));
        _results["unconfirmed"] = await handler.Send(socket, new QueryResult(), mutex, CancellationToken.None);
        _lockCounts.Add(mutex.CurrentCount);

        socket.Send(Arg.Any<ArraySegment<byte>>(), Arg.Any<System.Net.WebSockets.WebSocketMessageType>(), Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(Task.FromException(_error));
        _results["failed"] = await handler.Send(socket, new QueryResult(), mutex, CancellationToken.None);
        _legacyFailure = await handler.SendMessage(socket, new QueryResult(), mutex, CancellationToken.None);
        _lockCounts.Add(mutex.CurrentCount);

        var options = new ArcOptions();
        options.JsonSerializerOptions.Converters.Add(new a_throwing_converter(_error));
        var serializationHandler = new WebSocketConnectionHandler(Options.Create(options), Substitute.For<ILogger<WebSocketConnectionHandler>>());
        _results["serialization"] = await serializationHandler.Send(socket, new QueryResult(), mutex, CancellationToken.None);
        _lockCounts.Add(mutex.CurrentCount);
    }

    [Fact] void should_skip_closed_connections() => _results["closed"].Status.ShouldEqual(WebSocketSendStatus.Skipped);
    [Fact] void should_not_write_to_closed_connections() => _closedWrites.ShouldEqual(0);
    [Fact] void should_wait_for_the_lock() => _waitWasPending.ShouldBeTrue();
    [Fact] void should_recheck_the_socket_after_locking() => _results["closedAfterLock"].Status.ShouldEqual(WebSocketSendStatus.Skipped);
    [Fact] void should_skip_cancelled_lock_acquisition() => _results["cancelledWait"].Status.ShouldEqual(WebSocketSendStatus.Skipped);
    [Fact] void should_await_the_complete_transport_write() => _transportWasPending.ShouldBeTrue();
    [Fact] void should_confirm_writes_even_when_the_socket_closes_immediately() => _results["immediateClose"].Status.ShouldEqual(WebSocketSendStatus.Sent);
    [Fact] void should_skip_already_cancelled_writes() => _results["alreadyCancelled"].Status.ShouldEqual(WebSocketSendStatus.Skipped);
    [Fact] void should_confirm_completed_writes() => _results["sent"].Status.ShouldEqual(WebSocketSendStatus.Sent);
    [Fact] void should_confirm_awaited_writes() => _results["awaited"].Status.ShouldEqual(WebSocketSendStatus.Sent);
    [Fact] void should_leave_cancelled_transport_writes_unconfirmed() => _results["unconfirmed"].Status.ShouldEqual(WebSocketSendStatus.Unconfirmed);
    [Fact] void should_preserve_transport_failures() => _results["failed"].Error.ShouldEqual(_error);
    [Fact] void should_preserve_serialization_failures() => _results["serialization"].Error.ShouldEqual(_error);
    [Fact] void should_release_only_acquired_locks() => _lockCounts.ShouldEachConformTo(_ => _ == 1);
    [Fact] void should_keep_legacy_success_null() => _legacySuccess.ShouldBeNull();
    [Fact] void should_keep_legacy_failure() => _legacyFailure.ShouldEqual(_error);

    void Destroy()
    {
        _cancellation?.Dispose();
        _mutex?.Dispose();
    }

    static WebSocketConnectionHandler CreateHandler() => new(Options.Create(new ArcOptions()), Substitute.For<ILogger<WebSocketConnectionHandler>>());

    sealed class a_throwing_converter(Exception error) : JsonConverter<QueryResult>
    {
        public override QueryResult? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => throw error;
        public override void Write(Utf8JsonWriter writer, QueryResult value, JsonSerializerOptions options) => throw error;
    }
}
