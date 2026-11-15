// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.WebSockets;
using Cratis.Arc.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Queries.for_WebSocketSends;

public class when_dispatching_to_derived_handlers : Specification
{
    readonly InvalidOperationException _error = new("Custom send failed");
    IWebSocket _socket;
    WebSocketSendResult _builtIn;
    WebSocketSendResult _inheritedCapability;
    WebSocketSendResult _explicit;
    WebSocketSendResult _implicit;
    WebSocketSendResult _inheritedReimplementation;
    WebSocketSendResult _returnedError;
    WebSocketSendResult _thrownError;

    async Task Because()
    {
        _socket = Substitute.For<IWebSocket>();
        _socket.State.Returns(WebSocketState.Open);
        using var mutex = new SemaphoreSlim(1, 1);
        _builtIn = await Send(new WebSocketConnectionHandler(Options.Create(new ArcOptions()), Substitute.For<ILogger<WebSocketConnectionHandler>>()), mutex);
        _inheritedCapability = await Send(new inherits_capability(), mutex);
        _explicit = await Send(new explicit_reimplementation(), mutex);
        var custom = new implicit_reimplementation();
        _implicit = await Send(custom, mutex);
        _inheritedReimplementation = await Send(new inherits_reimplementation(), mutex);
        custom.Error = _error;
        _returnedError = await Send(custom, mutex);
        custom.Throws = true;
        _thrownError = await Send(custom, mutex);
    }

    [Fact] void should_confirm_the_built_in_send() => _builtIn.Status.ShouldEqual(WebSocketSendStatus.Sent);
    [Fact] void should_confirm_an_inherited_unmodified_send() => _inheritedCapability.Status.ShouldEqual(WebSocketSendStatus.Sent);
    [Fact] void should_leave_explicit_legacy_success_unconfirmed() => _explicit.Status.ShouldEqual(WebSocketSendStatus.Unconfirmed);
    [Fact] void should_leave_implicit_legacy_success_unconfirmed() => _implicit.Status.ShouldEqual(WebSocketSendStatus.Unconfirmed);
    [Fact] void should_honor_an_inherited_reimplementation() => _inheritedReimplementation.Status.ShouldEqual(WebSocketSendStatus.Unconfirmed);
    [Fact] void should_preserve_a_custom_returned_error() => _returnedError.Error.ShouldEqual(_error);
    [Fact] void should_capture_a_custom_thrown_error() => _thrownError.Error.ShouldEqual(_error);
    [Fact] void should_only_use_the_built_in_writer_for_unmodified_handlers() => _socket.Received(2).Send(Arg.Any<ArraySegment<byte>>(), System.Net.WebSockets.WebSocketMessageType.Text, true, Arg.Any<CancellationToken>());

    Task<WebSocketSendResult> Send(IWebSocketConnectionHandler handler, SemaphoreSlim mutex) =>
        WebSocketSends.Send(handler, _socket, new QueryResult(), mutex, CancellationToken.None);

    class inherits_capability() : WebSocketConnectionHandler(Options.Create(new ArcOptions()), Substitute.For<ILogger<WebSocketConnectionHandler>>());

    class implicit_reimplementation() : WebSocketConnectionHandler(Options.Create(new ArcOptions()), Substitute.For<ILogger<WebSocketConnectionHandler>>()), IWebSocketConnectionHandler
    {
        public Exception? Error { get; set; }
        public bool Throws { get; set; }

        public new Task<Exception?> SendMessage(IWebSocket webSocket, QueryResult queryResult, SemaphoreSlim writeLock, CancellationToken token) =>
            Throws ? throw Error! : Task.FromResult(Error);
    }

    class explicit_reimplementation() : WebSocketConnectionHandler(Options.Create(new ArcOptions()), Substitute.For<ILogger<WebSocketConnectionHandler>>()), IWebSocketConnectionHandler
    {
        Task<Exception?> IWebSocketConnectionHandler.SendMessage(IWebSocket webSocket, QueryResult queryResult, SemaphoreSlim writeLock, CancellationToken token) =>
            Task.FromResult<Exception?>(null);
    }

    class inherits_reimplementation : implicit_reimplementation;
}
