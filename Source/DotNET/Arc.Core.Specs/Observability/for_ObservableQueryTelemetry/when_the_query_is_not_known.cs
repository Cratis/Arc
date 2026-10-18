// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Observability.for_ObservableQueryTelemetry;

public class when_the_query_is_not_known : given.an_observable_query_telemetry
{
    const string Unknown = "MyApp.Secrets.Made.Up.By.The.Client";

    void Because()
    {
        var telemetry = CreateTelemetry(Unknown);
        telemetry.Start();
        using (var push = telemetry.BeginPush())
        {
            push.Delivered();
        }

        telemetry.Dispose();
    }

    [Fact] void should_count_the_subscription_as_other() => Subscriptions.ShouldEachConformTo(_ => (string)_.Tags[WellKnownTelemetryNames.QueryName]! == WellKnownTelemetryNames.Other);
    [Fact] void should_count_the_push_as_other() => Pushes.Single().Tags[WellKnownTelemetryNames.QueryName].ShouldEqual(WellKnownTelemetryNames.Other);
    [Fact] void should_tag_the_span_as_other() => _telemetry.Span(WellKnownTelemetryNames.QueryPushSpan).GetTagItem(WellKnownTelemetryNames.QueryName).ShouldEqual(WellKnownTelemetryNames.Other);
    [Fact] void should_never_record_the_name_it_was_sent() => _telemetry.AnyTagContains(Unknown).ShouldBeFalse();
}
