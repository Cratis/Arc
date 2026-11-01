// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Execution;

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.when_handling_sse_subscribe;

public class and_the_query_is_unauthorized : given.a_scoped_subscription_container
{
    void Establish() => _result = () => QueryResult.Unauthorized(CorrelationId.New());

    async Task Because() => await RunConnection(() => Task.CompletedTask);

    [Fact] void should_respond_unauthorized() => _subscribeStatusCodes[FirstQueryId].ShouldEqual(401);
    [Fact] void should_dispose_the_probe_exactly_once() => _resolvedDuringPerform.Single().Disposals.ShouldEqual(1);
}
