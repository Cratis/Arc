// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;

namespace Cratis.Arc.Queries.for_WebSocketSends;

public class when_dispatching : Specification
{
    readonly InvalidOperationException _error = new("Send failed");
    WebSocketSendResult _legacyNull;
    WebSocketSendResult _legacyError;
    WebSocketSendResult _legacyThrow;
    WebSocketSendResult _capability;
    WebSocketSendResult _capabilityThrow;
    IWebSocketConnectionHandler _capable;
    Exception? _nullFailure;

    async Task Because()
    {
        var socket = Substitute.For<IWebSocket>();
        using var mutex = new SemaphoreSlim(1, 1);
        var legacy = Substitute.For<IWebSocketConnectionHandler>();
        _legacyNull = await WebSocketSends.Send(legacy, socket, new QueryResult(), mutex, CancellationToken.None);
        legacy.SendMessage(Arg.Any<IWebSocket>(), Arg.Any<QueryResult>(), Arg.Any<SemaphoreSlim>(), Arg.Any<CancellationToken>()).Returns(_error);
        _legacyError = await WebSocketSends.Send(legacy, socket, new QueryResult(), mutex, CancellationToken.None);
        legacy.SendMessage(Arg.Any<IWebSocket>(), Arg.Any<QueryResult>(), Arg.Any<SemaphoreSlim>(), Arg.Any<CancellationToken>()).Returns<Exception?>(_ => throw _error);
        _legacyThrow = await WebSocketSends.Send(legacy, socket, new QueryResult(), mutex, CancellationToken.None);
        _capable = Substitute.For<IWebSocketConnectionHandler, IWebSocketMessageSender>();
        var sender = (IWebSocketMessageSender)_capable;
        sender.Send(Arg.Any<IWebSocket>(), Arg.Any<QueryResult>(), Arg.Any<SemaphoreSlim>(), Arg.Any<CancellationToken>()).Returns(WebSocketSendResult.Sent);
        _capability = await WebSocketSends.Send(_capable, socket, new QueryResult(), mutex, CancellationToken.None);
        sender.Send(Arg.Any<IWebSocket>(), Arg.Any<QueryResult>(), Arg.Any<SemaphoreSlim>(), Arg.Any<CancellationToken>()).Returns<WebSocketSendResult>(_ => throw _error);
        _capabilityThrow = await WebSocketSends.Send(_capable, socket, new QueryResult(), mutex, CancellationToken.None);
        _nullFailure = Catch.Exception(() => WebSocketSendResult.Failed(null!));
    }

    [Fact] void should_leave_legacy_null_unconfirmed() => _legacyNull.Status.ShouldEqual(WebSocketSendStatus.Unconfirmed);
    [Fact] void should_preserve_legacy_errors() => _legacyError.Error.ShouldEqual(_error);
    [Fact] void should_capture_legacy_throws() => _legacyThrow.Error.ShouldEqual(_error);
    [Fact] void should_use_the_capability() => _capability.Status.ShouldEqual(WebSocketSendStatus.Sent);
    [Fact] void should_capture_capability_throws() => _capabilityThrow.Error.ShouldEqual(_error);
    [Fact] void should_not_call_legacy_when_capable() => _capable.ReceivedCalls().Where(_ => _.GetMethodInfo().Name == nameof(IWebSocketConnectionHandler.SendMessage)).ShouldBeEmpty();
    [Fact] void should_default_to_unconfirmed() => default(WebSocketSendStatus).ShouldEqual(WebSocketSendStatus.Unconfirmed);
    [Fact] void should_reject_missing_failure_errors() => _nullFailure.ShouldBeOfExactType<ArgumentNullException>();
    [Fact] void should_not_attach_errors_to_success() => WebSocketSendResult.Sent.Error.ShouldBeNull();
    [Fact] void should_not_attach_errors_to_skips() => WebSocketSendResult.Skipped.Error.ShouldBeNull();
    [Fact] void should_not_attach_errors_to_unconfirmed_writes() => WebSocketSendResult.Unconfirmed.Error.ShouldBeNull();
}
