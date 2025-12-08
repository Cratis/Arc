// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;

namespace Cratis.Arc.Commands.ResponseValueHandlers;

/// <summary>
/// Represents an implementation of <see cref="ICommandResponseValueHandler"/> that handles <see cref="ValidationResult"/>.
/// </summary>
public class ValidationResultResponseValueHandler : ICommandResponseValueHandler
{
    /// <inheritdoc/>
    public bool CanHandle(CommandContext commandContext, object value) => value is ValidationResult;

    /// <inheritdoc/>
    public Task<CommandResult> Handle(CommandContext commandContext, object value) =>
        Task.FromResult(new CommandResult()
        {
            CorrelationId = commandContext.CorrelationId,
            ValidationResults = [(value as ValidationResult)!]
        });
}
