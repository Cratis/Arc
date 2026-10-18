// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ClientEnumerableObservableSSE.when_handling_connection;

public class and_items_are_pushed : given.a_telemetered_client_enumerable_observable_sse
{
    async Task Because() => await RunConnection();

    [Fact] void should_count_the_subscription_up_and_then_down() => _observed.Subscriptions.Select(_ => _.Value).ShouldContainOnly(1, -1);
    [Fact] void should_leave_no_subscription_active() => _observed.ActiveSubscriptions.ShouldEqual(0);
    [Fact] void should_count_a_push_for_each_item() => _observed.Pushes.Count().ShouldEqual(2);
    [Fact] void should_tag_the_pushes_with_the_query_name() => _observed.Pushes.ShouldEachConformTo(_ => (string)_.Tags[WellKnownTelemetryNames.QueryName]! == QueryName);
    [Fact] void should_tag_the_pushes_with_the_transport() => _observed.Pushes.ShouldEachConformTo(_ => (string)_.Tags[WellKnownTelemetryNames.QueryTransport]! == WellKnownTelemetryNames.ServerSentEventsTransport);
    [Fact] void should_raise_a_span_for_each_push() => _observed.PushSpans.Count().ShouldEqual(2);
}
