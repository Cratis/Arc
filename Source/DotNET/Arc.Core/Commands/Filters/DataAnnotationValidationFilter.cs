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
        var hasMembers = DataAnnotationMembers.TryGet(context.Command.GetType(), out var members);
        System.ComponentModel.DataAnnotations.Validator.TryValidateObject(context.Command, validationContext, validationResults, !hasMembers);
        if (hasMembers)
        {
            var names = members!.Select(member => member.Name).ToHashSet(StringComparer.Ordinal);
            validationResults.RemoveAll(result => result.MemberNames.Any() && result.MemberNames.All(names.Contains));
            foreach (var member in members)
            {
                validationContext.MemberName = member.Name;
                validationContext.DisplayName = member.DisplayName;
                var value = member.ReadValue(context.Command);
                foreach (var attribute in member.Attributes())
                {
                    if (attribute.GetValidationResult(value, validationContext) is { } result)
                    {
                        validationResults.Add(result);
                    }
                }
            }
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
}