// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Diagnostics.Metrics;
using Cratis.Arc.Observability;

namespace Cratis.Arc.Queries.for_QueryFilters.when_performing;

public class and_the_query_is_traced : given.a_query_filters
{
    Meter _meter;
    TelemetryRecorder _telemetry;

    void Establish()
    {
        _meter = new("Cratis.Arc.Test");
        _telemetry = new(_actualActivitySource, _meter);
        _queryFilters = new QueryFilters(new KnownInstancesOf<IQueryFilter>([]), _activitySource);
    }

    Task Because() => _queryFilters.OnPerform(_queryContext);

    void Destroy()
    {
        _telemetry.Dispose();
        _meter.Dispose();
    }

    Activity Span => _telemetry.Span("cratis.arc.query.filter");

    [Fact] void should_keep_the_legacy_query_name() => Span.GetTagItem("query_name").ShouldEqual(_queryContext.Name.Value);
    [Fact] void should_add_the_aligned_query_name() => Span.GetTagItem(WellKnownTelemetryNames.QueryName).ShouldEqual(_queryContext.Name.Value);
    [Fact] void should_keep_the_internal_kind() => Span.Kind.ShouldEqual(ActivityKind.Internal);
}
