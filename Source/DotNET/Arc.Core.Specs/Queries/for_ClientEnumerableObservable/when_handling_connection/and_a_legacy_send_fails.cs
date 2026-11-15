// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.WebSockets;

namespace Cratis.Arc.Queries.for_ClientEnumerableObservable.when_handling_connection;

public class and_a_legacy_send_fails : given.a_connection_with_send_outcomes
{
    Task Because() => Run(WebSocketSendResult.Failed(_error), legacy: true);

    [Fact] void should_stop_after_the_first_send() => _sends.ShouldEqual(1);
    [Fact] void should_not_drain_later_items() => _itemsRead.ShouldEqual(1);
    [Fact] void should_dispose_the_enumerator() => _enumeratorDisposed.ShouldBeTrue();
    [Fact] void should_cancel_the_connection() => _sendToken.IsCancellationRequested.ShouldBeTrue();
    [Fact] void should_close_the_socket() => _socket.Received(1).Close(WebSocketCloseStatus.InternalServerError, null, CancellationToken.None);
    [Fact] void should_not_count_a_push() => _observed.Pushes.ShouldBeEmpty();
}
