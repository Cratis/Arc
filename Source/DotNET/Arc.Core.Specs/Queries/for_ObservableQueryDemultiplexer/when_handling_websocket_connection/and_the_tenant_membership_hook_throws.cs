// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.when_handling_websocket_connection;

public class and_the_tenant_membership_hook_throws : given.a_tenant_admission
{
    void Establish() => _hookThrows = true;
    async Task Because()
    {
        BuildRequestServices();
        await _hub.HandleWebSocketConnection(_context);
    }
    [Fact] void should_respond_forbidden() => _context.Received(1).SetStatusCode(403);
    [Fact] void should_not_accept_the_socket() => _webSockets.DidNotReceive().AcceptWebSocket(Arg.Any<CancellationToken>());
}
