// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.when_handling_sse_subscribe;

public class and_the_query_throws : given.a_scoped_subscription_container
{
    void Establish() => _result = () => throw new InvalidOperationException("boom");

    async Task Because() => await RunConnection(() => Task.CompletedTask);

    [Fact] void should_deliver_the_error() => HasErrorFor(FirstQueryId).ShouldBeTrue();
    [Fact] void should_dispose_the_probe_exactly_once() => _resolvedDuringPerform.Single().Disposals.ShouldEqual(1);
}
