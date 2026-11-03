// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.when_handling_sse_subscribe;

public class and_a_streaming_subscription_is_unsubscribed : given.a_scoped_subscription_container
{
    int _disposalsWhileLive;

    async Task Because() => await RunConnection(async () =>
    {
        var probe = _resolvedDuringPerform.Single();
        _disposalsWhileLive = probe.Disposals;
        await Unsubscribe(FirstQueryId);
        await WaitFor(() => probe.Disposals > 0);
    });

    [Fact] void should_keep_the_scope_while_the_stream_is_live() => _disposalsWhileLive.ShouldEqual(0);
    [Fact] void should_dispose_the_probe_exactly_once_after_unsubscribe() => _resolvedDuringPerform.Single().Disposals.ShouldEqual(1);
}
