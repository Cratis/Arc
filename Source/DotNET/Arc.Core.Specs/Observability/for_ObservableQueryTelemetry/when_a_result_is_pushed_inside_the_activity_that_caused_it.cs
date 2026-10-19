// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Cratis.Arc.Observability.for_ObservableQueryTelemetry;

public class when_a_result_is_pushed_inside_the_activity_that_caused_it : given.an_observable_query_telemetry
{
    ActivityContext _causing;
    ActivityContext _subscribing;

    void Because()
    {
        using var subscribing = _source.StartActivity("subscribe");
        _subscribing = subscribing!.Context;
        var telemetry = CreateTelemetry(KnownQuery);
        subscribing.Stop();

        using var causing = new Activity("cause").Start();
        _causing = causing.Context;
        using var push = telemetry.BeginPush();
        push.Delivered();
    }

    Activity Span => _telemetry.Span(WellKnownTelemetryNames.QueryPushSpan);

    [Fact] void should_link_the_span_to_the_causing_activity() => Span.Links.Select(_ => _.Context).ShouldContainOnly(_causing);
    [Fact] void should_keep_the_subscription_as_the_parent() => Span.ParentSpanId.ShouldEqual(_subscribing.SpanId);
}
