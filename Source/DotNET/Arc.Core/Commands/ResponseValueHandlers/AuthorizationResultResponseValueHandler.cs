// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;

namespace Cratis.Arc.Commands.ResponseValueHandlers;

/// <summary>
/// Represents an implementation of <see cref="ICommandResponseValueHandler"/> that handles <see cref="AuthorizationResult"/>.
/// </summary>
public class AuthorizationResultResponseValueHandler : ICommandResponseValueHandler
{
    /// <inheritdoc/>
    public bool CanHandle(CommandContext commandContext, object value) => value is AuthorizationResult;

    /// <inheritdoc/>
    public Task<CommandResult> Handle(CommandContext commandContext, object value)
    {
        var authorizationResult = (AuthorizationResult)value;
        if (authorizationResult.IsAuthorized)
        {
            return Task.FromResult(new CommandResult()
            {
                CorrelationId = commandContext.CorrelationId
            });
        }

        return Task.FromResult(CommandResult.Unauthorized(commandContext.CorrelationId, authorizationResult.FailureReason));
    }
}