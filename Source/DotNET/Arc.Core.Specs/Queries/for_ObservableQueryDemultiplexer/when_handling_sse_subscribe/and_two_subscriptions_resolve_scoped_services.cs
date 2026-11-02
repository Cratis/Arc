// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.when_handling_sse_subscribe;

public class and_two_subscriptions_resolve_scoped_services : given.a_scoped_subscription_container
{
    void Establish() => _queryIds = [FirstQueryId, SecondQueryId];

    async Task Because() => await RunConnection(() => Task.CompletedTask);

    [Fact] void should_resolve_one_probe_per_subscription() => _resolvedDuringPerform.Distinct().Count().ShouldEqual(2);
    [Fact] void should_not_resolve_the_root_instance() => _resolvedDuringPerform.ShouldNotContain(_rootProbe);
    [Fact] void should_dispose_each_probe_exactly_once() => _resolvedDuringPerform.All(_ => _.Disposals == 1).ShouldBeTrue();
    [Fact] void should_not_dispose_the_root_instance() => _rootProbe.Disposals.ShouldEqual(0);
}
