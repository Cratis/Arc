// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.when_handling_sse_subscribe;

public class and_guard_denies_an_async_enumerable_emission : given.a_guarded_sse_connection
{
    void Establish()
    {
        _streamingData = ABatchEvery10Milliseconds();
        _verdict = _ => ObservableQueryEmissionVerdict.DenyAndTerminate;
    }

    async Task Because() => await RunConnection(async () =>
    {
        await WaitFor(() => HasUnauthorizedFor(FirstQueryId));
        await Task.Delay(50);
    });

    [Fact] void should_not_write_the_emission() => CountQueryResultsFor(FirstQueryId).ShouldEqual(0);
    [Fact] void should_signal_unauthorized() => HasUnauthorizedFor(FirstQueryId).ShouldBeTrue();
    [Fact] void should_stop_consulting_the_guard() => _guardCalls.Count.ShouldEqual(1);
}
