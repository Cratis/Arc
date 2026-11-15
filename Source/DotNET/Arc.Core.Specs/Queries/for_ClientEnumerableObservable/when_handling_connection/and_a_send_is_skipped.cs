// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.WebSockets;

namespace Cratis.Arc.Queries.for_ClientEnumerableObservable.when_handling_connection;

public class and_a_send_is_skipped : given.a_connection_with_send_outcomes
{
    Task Because() => Run(WebSocketSendResult.Skipped);

    [Fact] void should_continue_to_later_items() => _itemsRead.ShouldEqual(3);
    [Fact] void should_attempt_each_send() => _sends.ShouldEqual(3);
    [Fact] void should_dispose_the_enumerator() => _enumeratorDisposed.ShouldBeTrue();
    [Fact] void should_not_count_a_push() => _observed.Pushes.ShouldBeEmpty();
    [Fact] void should_not_close_the_socket_as_a_failure() => _socket.DidNotReceive().Close(WebSocketCloseStatus.InternalServerError, Arg.Any<string?>(), Arg.Any<CancellationToken>());
}
