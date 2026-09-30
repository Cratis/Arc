// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Cratis.Arc.Queries.for_QueryPipeline.when_performing;

public class with_a_traced_snapshot_query : given.a_traced_query_pipeline
{
    void Establish()
    {
        WithKnownQuery();
        var data = new List<object> { new() };
        _queryPerformer.Perform(Arg.Any<QueryContext>()).Returns(ValueTask.FromResult<object?>(data));
        _queryRenderers.Render(_queryName, data, _serviceProvider).Returns(new QueryRendererResult(1, data));
    }

    Task Because() => Perform();

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_name_the_span_after_the_query() => QuerySpan.DisplayName.ShouldEqual("AllAuthors");
    [Fact] void should_add_the_query_name() => QuerySpan.GetTagItem(WellKnownTelemetryNames.QueryName).ShouldEqual(_queryName.Value);
    [Fact] void should_add_the_snapshot_transport() => QuerySpan.GetTagItem(WellKnownTelemetryNames.QueryTransport).ShouldEqual(WellKnownTelemetryNames.SnapshotTransport);
    [Fact] void should_add_the_correlation_id() => QuerySpan.GetTagItem(WellKnownTelemetryNames.CorrelationId).ShouldEqual(_correlationId.ToString());
    [Fact] void should_add_the_outcome() => QuerySpan.GetTagItem(WellKnownTelemetryNames.QueryOutcome).ShouldEqual(WellKnownOperationOutcomes.Success);
    [Fact] void should_leave_the_status_unset() => QuerySpan.Status.ShouldEqual(ActivityStatusCode.Unset);
    [Fact] void should_record_the_duration_for_the_query() => Duration.Tags[WellKnownTelemetryNames.QueryName].ShouldEqual(_queryName.Value);
    [Fact] void should_record_the_duration_for_the_transport() => Duration.Tags[WellKnownTelemetryNames.QueryTransport].ShouldEqual(WellKnownTelemetryNames.SnapshotTransport);
    [Fact] void should_record_the_duration_as_a_success() => Duration.Tags[WellKnownTelemetryNames.QueryOutcome].ShouldEqual(WellKnownOperationOutcomes.Success);
}
