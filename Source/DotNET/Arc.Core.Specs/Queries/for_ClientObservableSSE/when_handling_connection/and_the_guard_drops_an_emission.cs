// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ClientObservableSSE.when_handling_connection;

public class and_the_guard_drops_an_emission : given.a_telemetered_client_observable_sse
{
    void Establish()
    {
        _emissionGuards.HasGuards.Returns(true);
        _verdict = _ => ObservableQueryEmissionVerdict.DenyAndTerminate;
    }

    async Task Because() => await RunConnection(async () =>
    {
        _subject.OnNext("event-store-a");
        await WaitFor(() => _messages.Count == 1);
        await Task.Delay(50);
    });

    [Fact] void should_not_count_a_push() => _observed.Pushes.ShouldBeEmpty();
    [Fact] void should_not_raise_a_push_span() => _observed.PushSpans.ShouldBeEmpty();
    [Fact] void should_still_end_the_subscription() => _observed.ActiveSubscriptions.ShouldEqual(0);
}
