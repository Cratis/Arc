// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries;

/// <summary>
/// Defines the non-generic entry Arc uses to call a query renderer.
/// </summary>
/// <remarks>
/// Implement <see cref="IQueryRendererFor{TQueryType}"/> rather than this interface directly.
/// </remarks>
public interface IQueryRenderer
{
    /// <summary>
    /// Render the query.
    /// </summary>
    /// <param name="query">Query to render. It is assignable to the query type of the renderer when Arc calls it.</param>
    /// <param name="queryContext">Context of the query.</param>
    /// <returns>The <see cref="QueryRendererResult"/>.</returns>
    QueryRendererResult Execute(object query, QueryContext queryContext);
}
