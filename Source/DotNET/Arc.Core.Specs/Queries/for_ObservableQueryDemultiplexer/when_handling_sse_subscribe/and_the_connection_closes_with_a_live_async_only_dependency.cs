// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.when_handling_sse_subscribe;

public class and_the_connection_closes_with_a_live_async_only_dependency : given.an_async_only_scoped_subscription_container
{
    int _disposalsWhileLive;

    async Task Because() => await RunConnection(() =>
    {
        _disposalsWhileLive = _asyncResolvedDuringPerform.Single().Disposals;
        return Task.CompletedTask;
    });

    [Fact] void should_keep_the_async_probe_while_live() => _disposalsWhileLive.ShouldEqual(0);
    [Fact] void should_dispose_the_async_probe_exactly_once() => _asyncResolvedDuringPerform.Single().Disposals.ShouldEqual(1);
    [Fact] void should_dispose_the_scoped_probe_exactly_once() => _resolvedDuringPerform.Single().Disposals.ShouldEqual(1);
}
