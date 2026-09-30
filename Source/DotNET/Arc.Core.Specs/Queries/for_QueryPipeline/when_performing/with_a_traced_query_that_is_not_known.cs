// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Cratis.Arc.Queries.for_QueryPipeline.when_performing;

/// <summary>
/// The query name arrives from the caller. A name that matches no query must not label anything, or a caller could
/// grow the metric series without bound.
/// </summary>
public class with_a_traced_query_that_is_not_known : given.a_traced_query_pipeline
{
    void Establish() =>
        _queryPerformerProviders.TryGetPerformersFor(_queryName, out var _).Returns(callInfo =>
        {
            callInfo[1] = null;
            return false;
        });

    Task Because() => Perform();

    [Fact] void should_fail() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_keep_the_generic_span_name() => QuerySpan.DisplayName.ShouldEqual(WellKnownTelemetryNames.QueryPerformSpan);
    [Fact] void should_not_add_the_query_name() => QuerySpan.GetTagItem(WellKnownTelemetryNames.QueryName).ShouldBeNull();
    [Fact] void should_set_the_status_to_error() => QuerySpan.Status.ShouldEqual(ActivityStatusCode.Error);
    [Fact] void should_record_the_duration_under_other() => Duration.Tags[WellKnownTelemetryNames.QueryName].ShouldEqual(WellKnownTelemetryNames.Other);
    [Fact] void should_record_the_duration_as_an_error() => Duration.Tags[WellKnownTelemetryNames.QueryOutcome].ShouldEqual(WellKnownOperationOutcomes.Error);
}
