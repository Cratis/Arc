// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Cratis.Arc.Queries.for_QueryPipeline.when_performing;

public class with_a_traced_query_the_caller_cancels : given.a_traced_query_pipeline
{
    CancellationTokenSource _cancellation;

    void Establish()
    {
        WithKnownQuery();
        _cancellation = new();
        _queryPerformer.Perform(Arg.Any<QueryContext>()).Returns<ValueTask<object?>>(_ => CancelWhilePerforming());
    }

    void Destroy() => _cancellation.Dispose();

    async Task Because() => _result = await _pipeline.Perform(_queryName, new(), Paging.NotPaged, Sorting.None, _serviceProvider, _cancellation.Token);

    async ValueTask<object?> CancelWhilePerforming()
    {
        await _cancellation.CancelAsync();
        throw new OperationCanceledException(_cancellation.Token);
    }

    [Fact] void should_add_the_cancelled_outcome() => QuerySpan.GetTagItem(WellKnownTelemetryNames.QueryOutcome).ShouldEqual(WellKnownOperationOutcomes.Cancelled);
    [Fact] void should_leave_the_status_unset() => QuerySpan.Status.ShouldEqual(ActivityStatusCode.Unset);
    [Fact] void should_record_the_cancellation() => QuerySpan.Events.Count(_ => _.Name == WellKnownTelemetryNames.ExceptionEvent).ShouldEqual(1);
    [Fact] void should_record_the_duration_as_cancelled() => Duration.Tags[WellKnownTelemetryNames.QueryOutcome].ShouldEqual(WellKnownOperationOutcomes.Cancelled);
}
