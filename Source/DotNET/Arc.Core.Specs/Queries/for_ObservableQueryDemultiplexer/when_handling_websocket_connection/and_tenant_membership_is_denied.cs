// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.when_handling_websocket_connection;

public class and_tenant_membership_is_denied : given.a_tenant_admission
{
    async Task Because()
    {
        BuildRequestServices();
        await _hub.HandleWebSocketConnection(_context);
    }
    [Fact] void should_respond_forbidden() => _context.Received(1).SetStatusCode(403);
    [Fact] void should_not_accept_the_socket() => _webSockets.DidNotReceive().AcceptWebSocket(Arg.Any<CancellationToken>());
    [Fact] void should_not_perform_a_query() => _queryPipeline.DidNotReceive().Perform(Arg.Any<FullyQualifiedQueryName>(), Arg.Any<QueryArguments>(), Arg.Any<Paging>(), Arg.Any<Sorting>(), Arg.Any<IServiceProvider>(), Arg.Any<CancellationToken>());
}
