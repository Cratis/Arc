// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.when_handling_websocket_connection;

public class and_no_tenant_membership_is_registered : given.a_tenant_admission
{
    void Establish()
    {
        _registerMembership = false;
        _webSockets.AcceptWebSocket(Arg.Any<CancellationToken>()).Returns(Task.FromResult(Substitute.For<IWebSocket>()));
    }
    async Task Because()
    {
        BuildRequestServices();
        await _hub.HandleWebSocketConnection(_context);
    }
    [Fact] void should_not_add_a_resolver_read() => _resolver.DidNotReceive().Resolve();
    [Fact] void should_preserve_websocket_admission() => _webSockets.Received(1).AcceptWebSocket(Arg.Any<CancellationToken>());
    [Fact] void should_not_deny_admission() => _context.DidNotReceive().SetStatusCode(403);
}
