// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries;

/// <summary>
/// Defines a system that can execute queries.
/// </summary>
public interface IQueryRenderers
{
    /// <summary>
    /// Render a query.
    /// </summary>
    /// <param name="queryName">Name of the query.</param>
    /// <param name="query">Query to render.</param>
    /// <param name="serviceProvider">The <see cref="IServiceProvider"/> scoped to the current request, used to resolve query renderer dependencies.</param>
    /// <returns>Result.</returns>
    QueryRendererResult Render(FullyQualifiedQueryName queryName, object query, IServiceProvider serviceProvider);
}
