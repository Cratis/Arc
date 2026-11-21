// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries.Filters;

namespace Cratis.Arc.Queries.for_QueryFilters.given;

public class two_blocking_query_filters : a_query_filters
{
    protected QueryResult _result;

    protected void PerformWith(params IQueryFilter[] discoveredFilters) =>
        _queryFilters = new QueryFilters(new KnownInstancesOf<IQueryFilter>(discoveredFilters), _activitySource);

    protected async Task Perform() => _result = await _queryFilters.OnPerform(_queryContext);

    protected class AFilter : IQueryFilter
    {
        public Task<QueryResult> OnPerform(QueryContext context) => Task.FromResult(QueryResult.Error(context.CorrelationId, "error from A"));
    }

    protected class BFilter : IQueryFilter
    {
        public Task<QueryResult> OnPerform(QueryContext context) => Task.FromResult(QueryResult.Error(context.CorrelationId, "error from B"));
    }
}
