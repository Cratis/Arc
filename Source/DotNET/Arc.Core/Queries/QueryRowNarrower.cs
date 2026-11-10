// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Queries;

/// <summary>
/// Applies the registered <see cref="IQueryRowNarrowing"/> implementations to the result of a query.
/// </summary>
static class QueryRowNarrower
{
    /// <summary>
    /// Narrows the result of a query, before it is sorted, paged and counted.
    /// </summary>
    /// <param name="queryName">The name of the query.</param>
    /// <param name="readModelType">The read model type the query returns.</param>
    /// <param name="data">The result the query performer returned.</param>
    /// <param name="context">The <see cref="QueryContext"/> of the query.</param>
    /// <param name="serviceProvider">The <see cref="IServiceProvider"/> to resolve narrowings from.</param>
    /// <returns>The narrowed result, or the same result when no narrowing applies.</returns>
    /// <exception cref="UnsupportedQueryRowNarrowing">A narrowing applies and the result cannot be narrowed.</exception>
    internal static async Task<object> Narrow(FullyQualifiedQueryName queryName, Type readModelType, object data, QueryContext context, IServiceProvider serviceProvider)
    {
        var predicates = new List<LambdaExpression>();
        foreach (var narrowing in serviceProvider.GetService<IEnumerable<IQueryRowNarrowing>>() ?? [])
        {
            if (await narrowing.GetPredicate(readModelType, context) is { } predicate)
            {
                predicates.Add(predicate);
            }
        }

        if (predicates.Count == 0)
        {
            return data;
        }

        if (data is IQueryable queryable)
        {
            return predicates.Aggregate(queryable, WhereInStore);
        }

        if (data is IEnumerable<object> rows and not IAsyncEnumerable<object>)
        {
            var compiled = predicates.ConvertAll(predicate => predicate.Compile());
            return rows.Where(row => compiled.TrueForAll(predicate => (bool)predicate.DynamicInvoke(row)!)).ToList();
        }

        throw new UnsupportedQueryRowNarrowing(queryName, data.GetType());
    }

    static IQueryable WhereInStore(IQueryable queryable, LambdaExpression predicate)
    {
        var call = Expression.Call(typeof(Queryable), nameof(Queryable.Where), [queryable.ElementType], queryable.Expression, Expression.Quote(predicate));
        return queryable.Provider.CreateQuery(call);
    }
}
