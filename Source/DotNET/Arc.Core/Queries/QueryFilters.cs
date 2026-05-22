// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.DependencyInjection;
using Cratis.Traces;
using Cratis.Types;

namespace Cratis.Arc.Queries;

/// <summary>
/// Represents an instance of <see cref="IQueryFilters"/>.
/// </summary>
/// <param name="filters">The collection of <see cref="IQueryFilter"/> to use for filtering queries.</param>
/// <param name="activitySource">The <see cref="IActivitySource{T}"/> for tracing.</param>
[Singleton]
public class QueryFilters(IInstancesOf<IQueryFilter> filters, IActivitySource<QueryFilters> activitySource) : IQueryFilters
{
    /// <inheritdoc/>
    public async Task<QueryResult> OnPerform(QueryContext context)
    {
        var result = QueryResult.Success(context.CorrelationId);
        using var span = activitySource.OnPerform(context.Name.Value);

        foreach (var filter in filters)
        {
            var filterResult = await filter.OnPerform(context);
            if (filterResult is not null)
            {
                result.MergeWith(filterResult);
            }
        }

        return result;
    }
}