// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_QueryFilters.when_performing;

public class with_multiple_filters_first_returns_null : given.a_query_filters
{
    IQueryFilter _firstFilter;
    IQueryFilter _secondFilter;
    QueryResult _secondFilterResult;
    QueryResult _result;

    void Establish()
    {
        _firstFilter = Substitute.For<IQueryFilter>();
        _secondFilter = Substitute.For<IQueryFilter>();
        _secondFilterResult = QueryResult.Error(_correlationId, "Second filter error");

        _firstFilter.OnPerform(_queryContext).Returns(Task.FromResult<QueryResult>(null!));
        _secondFilter.OnPerform(_queryContext).Returns(Task.FromResult(_secondFilterResult));

        _queryFilters = new QueryFilters(new KnownInstancesOf<IQueryFilter>([_firstFilter, _secondFilter]));
    }

    async Task Because() => _result = await _queryFilters.OnPerform(_queryContext);

    [Fact] void should_call_first_filter() => _firstFilter.Received(1).OnPerform(_queryContext);
    [Fact] void should_call_second_filter() => _secondFilter.Received(1).OnPerform(_queryContext);
    [Fact] void should_return_correlation_id() => _result.CorrelationId.ShouldEqual(_correlationId);
    [Fact] void should_merge_second_filter_result() => _result.ExceptionMessages.ShouldContain("Second filter error");
    [Fact] void should_not_be_success() => _result.IsSuccess.ShouldBeFalse();
}