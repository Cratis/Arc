// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Traces;

namespace Cratis.Arc.Queries.for_QueryPipeline.when_performing;

public class with_bound_context_values_and_staged_filters : given.a_query_with_bound_context_values
{
    void Establish()
    {
        var activitySource = Substitute.For<IActivitySource<QueryFilters>>();
        activitySource.ActualSource.Returns(_activitySource);
        query_filters = new QueryFilters(new KnownInstancesOf<IQueryFilter>([new Filters.AvailabilityFilter(_queryPerformerProviders)]), activitySource);
        var pipelineActivitySource = Substitute.For<IActivitySource<QueryPipeline>>();
        pipelineActivitySource.ActualSource.Returns(_activitySource);
        _pipeline = new QueryPipeline(
            _correlationIdAccessor,
            _queryContextManager,
            query_filters,
            _queryPerformerProviders,
            _queryRenderers,
            _readModelInterceptors,
            _discoverableValidators,
            pipelineActivitySource);
    }

    async Task Because() => await PerformInEnclosingOperation();

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_read_bound_values_in_validation() => _validatedValue.ShouldEqual("bound");
    [Fact] void should_read_bound_values_in_the_performer() => _performedValue.ShouldEqual("bound");
    [Fact] void should_read_each_bound_value() => _reads.Count.ShouldEqual(2);
    [Fact] void should_reuse_the_gates_invocation_in_validation() => _reads[0].ShouldBeSame(_gated);
    [Fact] void should_reuse_the_gates_invocation_in_the_performer() => _reads[1].ShouldBeSame(_gated);
    [Fact] void should_keep_the_coerced_arguments() => ((QueryArguments)_gated.Arguments)["count"].ShouldEqual(42);
    [Fact] void should_keep_the_receipt() => _gated.ReceivedAt.ShouldEqual(_receipt);
    [Fact] void should_use_the_querys_own_snapshot() => ReferenceEquals(_executionSnapshot, _outerSnapshot).ShouldBeFalse();
    [Fact] void should_not_overwrite_the_outer_context_values() => _outerValuesUnchanged.ShouldBeTrue();
    [Fact] void should_restore_the_outer_snapshot() => _outerSnapshotRestored.ShouldBeTrue();
}
