// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_QueryFilters.when_performing;

public class and_a_filter_throws : given.a_query_filters
{
    const string ThrownMessage = "the filter blew up";

    IQueryFilter _firstFilter;
    IQueryFilter _throwingFilter;
    IQueryFilter _laterFilter;
    QueryResult _result;

    void Establish()
    {
        _firstFilter = Substitute.For<IQueryFilter>();
        _throwingFilter = Substitute.For<IQueryFilter>();
        _laterFilter = Substitute.For<IQueryFilter>();

        _firstFilter.OnPerform(_queryContext).Returns(Task.FromResult(QueryResult.Success(_correlationId)));
        _throwingFilter.OnPerform(_queryContext).Returns<Task<QueryResult>>(_ => throw new InvalidOperationException(ThrownMessage));

        _queryFilters = new QueryFilters(new KnownInstancesOf<IQueryFilter>([_firstFilter, _throwingFilter, _laterFilter]), _activitySource);
    }

    async Task Because() => _result = await _queryFilters.OnPerform(_queryContext);

    [Fact] void should_call_the_first_filter() => _firstFilter.Received(1).OnPerform(_queryContext);
    [Fact] void should_call_the_throwing_filter() => _throwingFilter.Received(1).OnPerform(_queryContext);
    [Fact] void should_short_circuit_and_not_call_the_later_filter() => _laterFilter.DidNotReceive().OnPerform(_queryContext);
    [Fact] void should_carry_the_thrown_error() => _result.ExceptionMessages.ShouldContain(ThrownMessage);
    [Fact] void should_not_be_successful() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_preserve_the_prior_authorized_verdict() => _result.IsAuthorized.ShouldBeTrue();
}
