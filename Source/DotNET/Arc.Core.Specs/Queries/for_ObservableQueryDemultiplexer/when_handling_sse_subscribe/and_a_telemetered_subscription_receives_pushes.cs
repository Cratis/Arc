// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.when_handling_sse_subscribe;

public class and_a_telemetered_subscription_receives_pushes : given.a_telemetered_sse_subscription
{
    long _activeWhileSubscribed;
    int _pushesBeforeEmission;

    async Task Because() => await RunConnection(async () =>
    {
        await _hub.HandleSSESubscribe(SubscribeContextFor(_owner));
        await WaitFor(() => CountQueryResults() >= 1);
        _activeWhileSubscribed = _observed.ActiveSubscriptions;
        _pushesBeforeEmission = _observed.Pushes.Count();

        _subject.OnNext(["item-a"]);
        await WaitFor(() => _observed.Pushes.Count() > _pushesBeforeEmission);
        await WaitFor(() => _observed.PushSpans.Any());

        await _hub.HandleSSEUnsubscribe(UnsubscribeContextFor(_owner));
        await WaitFor(() => _observed.ActiveSubscriptions == 0);
    });

    [Fact] void should_count_the_subscription_while_it_is_open() => _activeWhileSubscribed.ShouldEqual(1);
    [Fact] void should_count_an_emission_as_a_push() => _observed.Pushes.Count().ShouldBeGreaterThan(_pushesBeforeEmission);
    [Fact] void should_tag_the_push_with_the_sse_transport() => _observed.Pushes.Last().Tags[WellKnownTelemetryNames.QueryTransport].ShouldEqual(WellKnownTelemetryNames.ServerSentEventsTransport);
    [Fact] void should_raise_a_push_span() => _observed.PushSpans.ShouldNotBeEmpty();
    [Fact] void should_stop_counting_the_subscription_once_it_ends() => _observed.ActiveSubscriptions.ShouldEqual(0);
}
