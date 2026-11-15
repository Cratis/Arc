// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.WebSockets;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Queries.for_ClientEnumerableObservable.when_handling_connection;

public class and_a_send_fails : given.a_connection_with_send_outcomes
{
    Task Because() => Run(WebSocketSendResult.Failed(_error));

    [Fact] void should_stop_after_the_first_send() => _sends.ShouldEqual(1);
    [Fact] void should_not_drain_later_items() => _itemsRead.ShouldEqual(1);
    [Fact] void should_dispose_the_enumerator() => _enumeratorDisposed.ShouldBeTrue();
    [Fact] void should_cancel_the_connection() => _sendToken.IsCancellationRequested.ShouldBeTrue();
    [Fact] void should_close_the_socket() => _socket.Received(1).Close(WebSocketCloseStatus.InternalServerError, null, CancellationToken.None);
    [Fact] void should_log_the_failure() => _logger.ReceivedCalls().ShouldContain(call => call.GetMethodInfo().Name == "Log" && Equals(call.GetArguments()[0], LogLevel.Warning) && ReferenceEquals(call.GetArguments()[3], _error));
    [Fact] void should_not_count_a_push() => _observed.Pushes.ShouldBeEmpty();
    [Fact] void should_end_the_subscription() => _observed.ActiveSubscriptions.ShouldEqual(0);
}
