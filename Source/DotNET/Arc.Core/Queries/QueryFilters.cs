// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.DependencyInjection;
using Cratis.Types;

namespace Cratis.Arc.Queries;

/// <summary>
/// Represents an instance of <see cref="IQueryFilters"/>.
/// </summary>
/// <param name="filters">The collection of <see cref="IQueryFilter"/> to use for filtering queries.</param>
[Singleton]
public class QueryFilters(IInstancesOf<IQueryFilter> filters) : IQueryFilters
{
    /// <inheritdoc/>
    public async Task<QueryResult> OnPerform(QueryContext context)
    {
        var result = QueryResult.Success(context.CorrelationId);

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