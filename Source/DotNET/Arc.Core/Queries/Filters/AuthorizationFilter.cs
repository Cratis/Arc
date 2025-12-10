// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;

namespace Cratis.Arc.Queries.Filters;

/// <summary>
/// Represents a query filter that authorizes queries before they are performed.
/// </summary>
/// <param name="authorizationEvaluator">The <see cref="IAuthorizationEvaluator"/> to use for authorization checks.</param>
/// <param name="queryPerformerProviders">The <see cref="IQueryPerformerProviders"/> to use for finding query performers.</param>
public class AuthorizationFilter(IAuthorizationEvaluator authorizationEvaluator, IQueryPerformerProviders queryPerformerProviders) : IQueryFilter
{
    /// <inheritdoc/>
    public Task<QueryResult> OnPerform(QueryContext context)
    {
        if (!queryPerformerProviders.TryGetPerformersFor(context.Name, out var performer))
        {
            return Task.FromResult(QueryResult.Success(context.CorrelationId));
        }

        var typeAuthorized = authorizationEvaluator.IsAuthorized(performer.Type);
        var performerAuthorized = performer.IsAuthorized(context);

        if (typeAuthorized && performerAuthorized)
        {
            return Task.FromResult(QueryResult.Success(context.CorrelationId));
        }

        return Task.FromResult(QueryResult.Unauthorized(context.CorrelationId));
    }
}