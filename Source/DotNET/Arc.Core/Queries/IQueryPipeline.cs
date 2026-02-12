// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries;

/// <summary>
/// Defines a system can execute queries.
/// </summary>
public interface IQueryPipeline
{
    /// <summary>
    /// Performs the given query.
    /// </summary>
    /// <param name="queryName">The name of the query to perform.</param>
    /// <param name="arguments">The arguments for the query.</param>
    /// <param name="paging">The paging to apply to the query.</param>
    /// <param name="sorting">The sorting to apply to the query.</param>
    /// <param name="serviceProvider">The <see cref="IServiceProvider"/> scoped to the current request, used to resolve dependencies.</param>
    /// <returns>A <see cref="QueryResult"/> representing the result of executing the command.</returns>
    Task<QueryResult> Perform(FullyQualifiedQueryName queryName, QueryArguments arguments, Paging paging, Sorting sorting, IServiceProvider serviceProvider);
}
