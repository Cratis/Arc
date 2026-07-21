// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;

namespace Cratis.Arc.Commands.Filters;

/// <summary>
/// Represents a command filter that validates commands before they are handled.
/// </summary>
/// <param name="modelGraphValidator">The <see cref="IModelGraphValidator"/> to validate the command graph with.</param>
public class FluentValidationFilter(IModelGraphValidator modelGraphValidator) : ICommandFilter
{
    /// <inheritdoc/>
    public async Task<CommandResult> OnExecution(CommandContext context)
    {
        var validationResults = await modelGraphValidator.Validate(
            new ModelGraphValidationRequest(context.Command, context.ServiceProvider),
            context.CancellationToken);

        var commandResult = CommandResult.Success(context.CorrelationId);
        commandResult.ValidationResults = [.. validationResults];
        return commandResult;
    }
}
