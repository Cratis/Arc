// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Cratis.Arc.Observability.for_ObservableQueryTelemetry;

public class when_a_result_is_pushed : given.an_observable_query_telemetry
{
    void Because()
    {
        var telemetry = CreateTelemetry(KnownQuery);
        using var push = telemetry.BeginPush();
        push.Delivered();
    }

    Activity Span => _telemetry.Span(WellKnownTelemetryNames.QueryPushSpan);

    [Fact] void should_count_one_push() => Pushes.Select(_ => _.Value).ShouldContainOnly(1);
    [Fact] void should_tag_the_push_with_the_query_name() => Pushes.Single().Tags[WellKnownTelemetryNames.QueryName].ShouldEqual(KnownQuery);
    [Fact] void should_tag_the_push_with_the_transport() => Pushes.Single().Tags[WellKnownTelemetryNames.QueryTransport].ShouldEqual(Transport);
    [Fact] void should_raise_one_internal_span() => Span.Kind.ShouldEqual(ActivityKind.Internal);
    [Fact] void should_tag_the_span_with_the_query_name() => Span.GetTagItem(WellKnownTelemetryNames.QueryName).ShouldEqual(KnownQuery);
    [Fact] void should_tag_the_span_with_the_transport() => Span.GetTagItem(WellKnownTelemetryNames.QueryTransport).ShouldEqual(Transport);
    [Fact] void should_not_link_the_span_when_nothing_caused_it() => Span.Links.ShouldBeEmpty();
}
