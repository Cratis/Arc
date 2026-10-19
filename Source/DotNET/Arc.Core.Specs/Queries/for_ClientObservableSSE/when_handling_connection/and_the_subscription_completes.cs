// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ClientObservableSSE.when_handling_connection;

public class and_the_subscription_completes : given.a_telemetered_client_observable_sse
{
    async Task Because() => await RunConnection(() => Task.CompletedTask);

    [Fact] void should_count_the_subscription_up_and_then_down() => _observed.Subscriptions.Select(_ => _.Value).ShouldContainOnly(1, -1);
    [Fact] void should_leave_no_subscription_active() => _observed.ActiveSubscriptions.ShouldEqual(0);
    [Fact] void should_tag_the_transport() => _observed.Subscriptions.ShouldEachConformTo(_ => (string)_.Tags[WellKnownTelemetryNames.QueryTransport]! == WellKnownTelemetryNames.ServerSentEventsTransport);
    [Fact] void should_not_count_a_push() => _observed.Pushes.ShouldBeEmpty();
}
