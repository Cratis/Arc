// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;

namespace Cratis.Arc.Commands.Filters;

/// <summary>
/// Represents a command filter that validates commands that has values adorned with data annotations.
/// </summary>
public class DataAnnotationValidationFilter : ICommandFilter
{
    /// <inheritdoc/>
    public Task<CommandResult> OnExecution(CommandContext context)
    {
        var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var validationContext = new System.ComponentModel.DataAnnotations.ValidationContext(context.Command);
        var isValid = System.ComponentModel.DataAnnotations.Validator.TryValidateObject(context.Command, validationContext, validationResults, true);
        if (!isValid)
        {
            return Task.FromResult(new CommandResult
            {
                CorrelationId = context.CorrelationId,
                IsAuthorized = true,
                ValidationResults = validationResults.Select(_ =>
                    new ValidationResult(ValidationResultSeverity.Error, _.ErrorMessage ?? "Validation error", _.MemberNames, null!)).ToArray()
            });
        }

        return Task.FromResult(CommandResult.Success(context.CorrelationId));
    }
}