// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_QueryFilters.when_performing;

public class with_single_filter_returning_null : given.a_query_filters
{
    IQueryFilter _filter;
    QueryResult _result;

    void Establish()
    {
        _filter = Substitute.For<IQueryFilter>();
        _filter.OnPerform(_queryContext).Returns(Task.FromResult<QueryResult>(null!));
        _queryFilters = new QueryFilters(new KnownInstancesOf<IQueryFilter>([_filter]));
    }

    async Task Because() => _result = await _queryFilters.OnPerform(_queryContext);

    [Fact] void should_call_filter() => _filter.Received(1).OnPerform(_queryContext);
    [Fact] void should_return_success_result() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_return_correlation_id() => _result.CorrelationId.ShouldEqual(_correlationId);
}