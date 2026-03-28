// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.Filters;

/// <summary>
/// Represents a query filter that authorizes queries before they are performed.
/// </summary>
/// <param name="queryPerformerProviders">The <see cref="IQueryPerformerProviders"/> to use for finding query performers.</param>
public class AuthorizationFilter(IQueryPerformerProviders queryPerformerProviders) : IQueryFilter
{
    /// <inheritdoc/>
    public Task<QueryResult> OnPerform(QueryContext context)
    {
        if (!queryPerformerProviders.TryGetPerformersFor(context.Name, out var performer))
        {
            return Task.FromResult(QueryResult.Success(context.CorrelationId));
        }

        // performer.IsAuthorized already applies the full hierarchy:
        // method-level [AllowAnonymous] overrides type-level [Authorize],
        // and a method with no annotation falls back to the type-level check.
        if (!performer.IsAuthorized(context))
        {
            return Task.FromResult(QueryResult.Unauthorized(context.CorrelationId));
        }

        return Task.FromResult(QueryResult.Success(context.CorrelationId));
    }
}