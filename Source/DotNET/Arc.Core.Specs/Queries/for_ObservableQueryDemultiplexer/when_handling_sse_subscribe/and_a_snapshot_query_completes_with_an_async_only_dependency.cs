// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Execution;

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.when_handling_sse_subscribe;

public class and_a_snapshot_query_completes_with_an_async_only_dependency : given.an_async_only_scoped_subscription_container
{
    int _disposalsWhenSubscribeReturned;

    void Establish() => _result = () =>
    {
        var result = QueryResult.Success(CorrelationId.New());
        result.Data = "snapshot";
        return result;
    };

    async Task Because() => await RunConnection(() =>
    {
        _disposalsWhenSubscribeReturned = _asyncResolvedDuringPerform.Single().Disposals;
        return Task.CompletedTask;
    });

    [Fact] void should_dispose_the_async_probe_when_subscribe_returns() => _disposalsWhenSubscribeReturned.ShouldEqual(1);
    [Fact] void should_dispose_the_async_probe_exactly_once() => _asyncResolvedDuringPerform.Single().Disposals.ShouldEqual(1);
    [Fact] void should_dispose_the_scoped_probe_exactly_once() => _resolvedDuringPerform.Single().Disposals.ShouldEqual(1);
}
