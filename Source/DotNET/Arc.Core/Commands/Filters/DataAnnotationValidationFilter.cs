// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;
using Cratis.Arc.Validation;

using SystemValidationResult = System.ComponentModel.DataAnnotations.ValidationResult;
using ValidationResult = Cratis.Arc.Validation.ValidationResult;

namespace Cratis.Arc.Commands.Filters;

/// <summary>
/// Represents a command filter that validates commands that has values adorned with data annotations.
/// </summary>
public class DataAnnotationValidationFilter : ICommandFilter
{
    /// <inheritdoc/>
    public Task<CommandResult> OnExecution(CommandContext context)
    {
        var validationResults = new List<SystemValidationResult>();
        var validationContext = new ValidationContext(context.Command);
        if (DataAnnotationMembers.TryGet(context.Command.GetType(), out var members, out var attributes))
        {
            ValidateRegistered(validationContext, validationResults, members, attributes);
        }
        else
        {
            Validator.TryValidateObject(context.Command, validationContext, validationResults, true);
        }

        if (validationResults.Count > 0)
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

    static void ValidateRegistered(ValidationContext context, List<SystemValidationResult> results, DataAnnotationMember[] members, Func<ValidationAttribute[]> attributes)
    {
        var command = context.ObjectInstance;
        var displayName = context.DisplayName;
        foreach (var member in members)
        {
            context.MemberName = member.Name;
            context.DisplayName = member.DisplayName;
            Validator.TryValidateValue(member.ReadValue(command), context, results, member.Attributes());
        }

        context.MemberName = null;
        context.DisplayName = displayName;
        if (results.Count > 0)
        {
            return;
        }

        ValidateObjectLevel(command, context, results, attributes);
    }

    static void ValidateObjectLevel(object command, ValidationContext context, List<SystemValidationResult> results, Func<ValidationAttribute[]> attributes)
    {
        results.AddRange(attributes().Select(attribute => attribute.GetValidationResult(command, context)).OfType<SystemValidationResult>());
        if (!CanValidateObject(command, results))
        {
            return;
        }

        var objectResults = ((IValidatableObject)command).Validate(context) ?? [];
        results.AddRange(objectResults.Where(result => result is not null));
    }

    static bool CanValidateObject(object command, List<SystemValidationResult> results) =>
        results.Count == 0 && command is IValidatableObject;
}