// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Observability.for_ObservableQueryTelemetry;

public class when_a_subscription_starts_and_ends : given.an_observable_query_telemetry
{
    void Because()
    {
        var telemetry = CreateTelemetry(KnownQuery);
        telemetry.Start();
        telemetry.Dispose();
        telemetry.Dispose();
    }

    [Fact] void should_count_the_subscription_up_and_down_once_each() => Subscriptions.Select(_ => _.Value).ShouldContainOnly(1, -1);
    [Fact] void should_tag_the_query_name() => Subscriptions.ShouldEachConformTo(_ => (string)_.Tags[WellKnownTelemetryNames.QueryName]! == KnownQuery);
    [Fact] void should_tag_the_transport() => Subscriptions.ShouldEachConformTo(_ => (string)_.Tags[WellKnownTelemetryNames.QueryTransport]! == Transport);
    [Fact] void should_not_count_a_push() => Pushes.ShouldBeEmpty();
}
