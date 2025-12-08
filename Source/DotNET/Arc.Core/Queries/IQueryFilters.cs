// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries;

/// <summary>
/// Represents a collection of <see cref="IQueryFilter"/> instances.
/// </summary>
public interface IQueryFilters
{
    /// <summary>
    /// Called when a query is performed.
    /// </summary>
    /// <param name="context">The <see cref="QueryContext"/> for the query being performed.</param>
    /// <returns>The <see cref="QueryResult"/>.</returns>
    Task<QueryResult> OnPerform(QueryContext context);
}
