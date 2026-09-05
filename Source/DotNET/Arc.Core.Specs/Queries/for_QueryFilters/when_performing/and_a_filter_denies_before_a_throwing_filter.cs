// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Arc.Http;

namespace Cratis.Arc.Queries.for_QueryFilters.when_performing;

public class and_a_filter_denies_before_a_throwing_filter : given.a_query_filters
{
    IQueryFilter _denyingFilter;
    IQueryFilter _throwingFilter;
    QueryResult _result;

    void Establish()
    {
        _denyingFilter = Substitute.For<IQueryFilter>();
        _throwingFilter = Substitute.For<IQueryFilter>();

        _denyingFilter.OnPerform(_queryContext).Returns(Task.FromResult(QueryResult.Unauthorized(_correlationId)));

        // A later filter would throw if it ran — the chain must short-circuit on the denial before reaching it,
        // so the throw never happens and the clean 403 is not converted into a 500.
        _throwingFilter.OnPerform(_queryContext).Returns<Task<QueryResult>>(_ => throw new InvalidOperationException("filter dereferenced a null concept"));

        _queryFilters = new QueryFilters(new KnownInstancesOf<IQueryFilter>([_denyingFilter, _throwingFilter]), _activitySource);
    }

    async Task Because() => _result = await _queryFilters.OnPerform(_queryContext);

    [Fact] void should_call_the_denying_filter() => _denyingFilter.Received(1).OnPerform(_queryContext);
    [Fact] void should_not_call_the_throwing_filter() => _throwingFilter.DidNotReceive().OnPerform(_queryContext);
    [Fact] void should_not_be_authorized() => _result.IsAuthorized.ShouldBeFalse();
    [Fact] void should_not_be_successful() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_not_carry_any_exception() => _result.HasExceptions.ShouldBeFalse();
    [Fact] void should_map_to_forbidden_and_not_internal_server_error() => EndpointRouteHelper.GetStatusCode(_result.IsSuccess, _result.IsAuthorized, _result.IsValid).ShouldEqual(HttpStatusCode.Forbidden);
}
