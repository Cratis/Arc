// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.when_handling_sse_subscribe;

public class and_the_connection_closes_with_a_live_subscription : given.a_scoped_subscription_container
{
    int _disposalsWhileLive;

    async Task Because() => await RunConnection(() =>
    {
        _disposalsWhileLive = _resolvedDuringPerform.Single().Disposals;
        return Task.CompletedTask;
    });

    [Fact] void should_keep_the_scope_while_the_connection_is_open() => _disposalsWhileLive.ShouldEqual(0);
    [Fact] void should_dispose_the_probe_exactly_once_after_the_connection_ends() => _resolvedDuringPerform.Single().Disposals.ShouldEqual(1);
}
