// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ClientObservable.when_handling_connection;

public class and_the_query_is_not_known : given.a_telemetered_client_observable
{
    void Establish() => _observed.KnownQueries.Clear();

    async Task Because() => await RunConnection(async () =>
    {
        _subject.OnNext("event-store-a");
        await WaitFor(() => _sent.Count == 1);
    });

    [Fact] void should_record_the_subscription_as_other() => _observed.Subscriptions.ShouldEachConformTo(_ => (string)_.Tags[WellKnownTelemetryNames.QueryName]! == WellKnownTelemetryNames.Other);
    [Fact] void should_record_the_push_as_other() => _observed.Pushes.Single().Tags[WellKnownTelemetryNames.QueryName].ShouldEqual(WellKnownTelemetryNames.Other);
    [Fact] void should_never_record_the_name() => _observed.Telemetry.AnyTagContains(QueryName).ShouldBeFalse();
}
