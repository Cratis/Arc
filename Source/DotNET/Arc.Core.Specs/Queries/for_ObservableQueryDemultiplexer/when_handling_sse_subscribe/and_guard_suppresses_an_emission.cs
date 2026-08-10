// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.when_handling_sse_subscribe;

public class and_guard_suppresses_an_emission : given.a_guarded_sse_connection
{
    void Establish() => _verdict = _ => ObservableQueryEmissionVerdict.Suppress;

    async Task Because() => await RunConnection(async () =>
    {
        _subject.OnNext(["event-store-a"]);
        await WaitFor(() => _guardCalls.Count == 1);

        _subject.OnNext(["event-store-b"]);
        await WaitFor(() => _guardCalls.Count == 2);
    });

    [Fact] void should_not_write_the_emission() => CountQueryResultsFor(FirstQueryId).ShouldEqual(0);
    [Fact] void should_not_signal_unauthorized() => HasUnauthorizedFor(FirstQueryId).ShouldBeFalse();
    [Fact] void should_keep_the_subscription_live() => _guardCalls.Count.ShouldEqual(2);
}
