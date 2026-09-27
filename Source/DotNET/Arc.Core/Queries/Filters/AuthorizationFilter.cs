// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Queries.Filters;

/// <summary>
/// Represents a query filter that authorizes queries before they are performed.
/// </summary>
/// <param name="queryPerformerProviders">The <see cref="IQueryPerformerProviders"/> to use for finding query performers.</param>
public class AuthorizationFilter(IQueryPerformerProviders queryPerformerProviders) : IAuthorizationQueryFilter
{
    /// <inheritdoc/>
    public async Task<QueryResult> OnPerform(QueryContext context)
    {
        context.CancellationToken.ThrowIfCancellationRequested();
        if (!queryPerformerProviders.TryGetPerformersFor(context.Name, out var performer))
        {
            return QueryResult.Success(context.CorrelationId);
        }

        if (context.ServiceProvider is null)
        {
            var typeRequiresScope = performer.Type is { } type && AuthorizationAttributeGuard.RequiresScopedEvaluation(type);
            var methodRequiresScope = performer is IAuthorizationQueryTarget declared &&
                AuthorizationAttributeGuard.RequiresScopedEvaluation(declared.AuthorizationMethod);
            var opaqueMethodRequiresScope = performer is not IAuthorizationQueryTarget &&
                performer.Type is { } opaqueType && AuthorizationAttributeGuard.HasAdvancedMethod(opaqueType);
            if (typeRequiresScope || methodRequiresScope || opaqueMethodRequiresScope)
            {
                return QueryResult.Unauthorized(context.CorrelationId);
            }
        }

        bool allowed;
        if (context.ServiceProvider is { } services)
        {
            var declarations = services.GetRequiredService<AuthorizationDeclarations>();
            var target = QueryAuthorizationTarget.For(performer, declarations);
            Func<bool>? legacyVerdict = performer is IFrameworkAuthorizationQueryTarget { HasIndependentLegacyVerdict: false }
                ? null
                : () => performer.IsAuthorized(context);
            allowed = await services.GetRequiredService<AuthorizationEvaluation>().IsAuthorized(
                target,
                context,
                services,
                context.CancellationToken,
                legacyVerdict);
        }
        else
        {
            try
            {
                allowed = performer.IsAuthorized(context);
            }
            catch (AsynchronousAuthorizationRequired)
            {
                context.CancellationToken.ThrowIfCancellationRequested();
                return QueryResult.Unauthorized(context.CorrelationId);
            }
        }

        context.CancellationToken.ThrowIfCancellationRequested();
        return allowed ? QueryResult.Success(context.CorrelationId) : QueryResult.Unauthorized(context.CorrelationId);
    }
}