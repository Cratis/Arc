// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Execution;

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.when_handling_sse_subscribe;

public class and_a_snapshot_query_completes : given.a_scoped_subscription_container
{
    int _disposalsWhenSubscribeReturned;

    void Establish() => _result = () =>
    {
        var result = QueryResult.Success(CorrelationId.New());
        result.Data = "snapshot";
        return result;
    };

    async Task Because() => await RunConnection(async () =>
    {
        _disposalsWhenSubscribeReturned = _resolvedDuringPerform.Single().Disposals;
        await Task.CompletedTask;
    });

    [Fact] void should_have_disposed_the_scope_when_subscribe_returned() => _disposalsWhenSubscribeReturned.ShouldEqual(1);
    [Fact] void should_dispose_the_probe_exactly_once() => _resolvedDuringPerform.Single().Disposals.ShouldEqual(1);
}
