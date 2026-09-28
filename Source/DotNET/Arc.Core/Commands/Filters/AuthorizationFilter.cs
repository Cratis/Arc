// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Commands.Filters;

/// <summary>
/// Represents a command filter that authorizes commands before they are handled.
/// </summary>
/// <param name="authorizationHelper">The <see cref="IAuthorizationEvaluator"/> to use for authorization checks.</param>
public class AuthorizationFilter(IAuthorizationEvaluator authorizationHelper) : IAuthorizationCommandFilter
{
    /// <inheritdoc/>
    public async Task<CommandResult> OnExecution(CommandContext context)
    {
        context.CancellationToken.ThrowIfCancellationRequested();
        if (context.ServiceProvider is null && AuthorizationAttributeGuard.RequiresScopedEvaluation(context.Type))
        {
            return CommandResult.Unauthorized(context.CorrelationId);
        }

        bool allowed;
        try
        {
            if (context.ServiceProvider is { } services)
            {
                _ = services.GetService<AuthorizationDeclarations>() ??
                    throw new InvalidAuthorizationConfiguration("Authorization declarations are unavailable.");
                var evaluation = services.GetService<AuthorizationEvaluation>() ??
                    throw new InvalidAuthorizationConfiguration("Authorization evaluation is unavailable.");
                allowed = await evaluation.IsAuthorized(context.Type, context, services, null, authorizationHelper, context.CancellationToken);
            }
            else
            {
                allowed = authorizationHelper.IsAuthorized(context.Type);
            }
        }
        catch (AsynchronousAuthorizationRequired)
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            return CommandResult.Unauthorized(context.CorrelationId);
        }
        context.CancellationToken.ThrowIfCancellationRequested();
        return allowed ? CommandResult.Success(context.CorrelationId) : CommandResult.Unauthorized(context.CorrelationId);
    }
}