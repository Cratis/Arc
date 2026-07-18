// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_QueryFilters.when_performing;

public class and_a_blocking_filter_short_circuits_later_filters : given.a_query_filters
{
    IQueryFilter _blockingFilter;
    IQueryFilter _laterFilter;
    QueryResult _result;

    void Establish()
    {
        _blockingFilter = Substitute.For<IQueryFilter>();
        _laterFilter = Substitute.For<IQueryFilter>();

        _blockingFilter.OnPerform(_queryContext).Returns(Task.FromResult(QueryResult.Error(_correlationId, "blocked")));

        _queryFilters = new QueryFilters(new KnownInstancesOf<IQueryFilter>([_blockingFilter, _laterFilter]), _activitySource);
    }

    async Task Because() => _result = await _queryFilters.OnPerform(_queryContext);

    [Fact] void should_call_the_blocking_filter() => _blockingFilter.Received(1).OnPerform(_queryContext);
    [Fact] void should_short_circuit_and_not_call_the_later_filter() => _laterFilter.DidNotReceive().OnPerform(_queryContext);
    [Fact] void should_carry_the_blocking_verdict() => _result.ExceptionMessages.ShouldContain("blocked");
    [Fact] void should_not_be_success() => _result.IsSuccess.ShouldBeFalse();
}
