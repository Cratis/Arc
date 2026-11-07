// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries;

/// <summary>A test-only proxy base for the internal staged-query-filter seam.</summary>
public class StagedQueryFiltersForSpecs : IStagedQueryFilters
{
    /// <inheritdoc/>
    public virtual Task<QueryResult> OnPerform(QueryContext context) => Task.FromResult(new QueryResult());

    /// <inheritdoc/>
    public Task<QueryResult> Authorize(QueryContext context) => AuthorizeQuery(context);

    /// <inheritdoc/>
    public Task<QueryResult> AfterAuthorization(QueryContext context) => FilterAuthorizedQuery(context);

    /// <summary>Supplies the authorization result configured by a spec.</summary>
    /// <param name="context">The query context.</param>
    /// <returns>The authorization result.</returns>
    public virtual Task<QueryResult> AuthorizeQuery(QueryContext context) => Task.FromResult(new QueryResult());

    /// <summary>Supplies the ordinary-filter result configured by a spec.</summary>
    /// <param name="context">The query context.</param>
    /// <returns>The filter result.</returns>
    public virtual Task<QueryResult> FilterAuthorizedQuery(QueryContext context) => Task.FromResult(new QueryResult());
}
