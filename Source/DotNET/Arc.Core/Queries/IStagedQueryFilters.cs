// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries;

/// <summary>
/// Runs authorization before query dependency construction, then ordinary filters with those dependencies available.
/// </summary>
internal interface IStagedQueryFilters : IQueryFilters
{
    /// <summary>
    /// Runs authorization filters before query dependencies are constructed.
    /// </summary>
    /// <param name="context">The query context.</param>
    /// <returns>The authorization verdict.</returns>
    Task<QueryResult> Authorize(QueryContext context);

    /// <summary>
    /// Runs ordinary filters once authorized dependencies have been constructed.
    /// </summary>
    /// <param name="context">The query context with dependencies.</param>
    /// <returns>The combined ordinary-filter result.</returns>
    Task<QueryResult> AfterAuthorization(QueryContext context);
}
