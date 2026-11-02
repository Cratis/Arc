// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.when_handling_websocket_connection;

public class and_the_client_disconnects_with_a_live_subscription : given.a_scoped_websocket_container
{
    int _disposalsWhileLive;

    async Task Because() => await RunConnection(async () =>
    {
        await WaitFor(() => !_resolvedDuringPerform.IsEmpty);
        _disposalsWhileLive = _resolvedDuringPerform.Single().Disposals;
    });

    [Fact] void should_keep_the_scope_while_the_connection_is_open() => _disposalsWhileLive.ShouldEqual(0);
    [Fact] void should_dispose_the_probe_exactly_once_after_the_connection_ends() => _resolvedDuringPerform.Single().Disposals.ShouldEqual(1);
}
