// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Cratis.Arc.Validation;
using CratisValidationResult = Cratis.Arc.Validation.ValidationResult;
using SystemValidationResult = System.ComponentModel.DataAnnotations.ValidationResult;

namespace Cratis.Arc.Queries.Filters;

/// <summary>
/// Represents a query filter that validates queries that has parameter values adorned with data annotations.
/// </summary>
/// <param name="queryPerformerProviders">The <see cref="IQueryPerformerProviders"/> to use for finding query performers.</param>
public class DataAnnotationValidationFilter(IQueryPerformerProviders queryPerformerProviders) : IQueryFilter
{
    /// <inheritdoc/>
    public Task<QueryResult> OnPerform(QueryContext context)
    {
        var queryResult = QueryResult.Success(context.CorrelationId);

        // Get the query performer to understand the parameter types
        if (!queryPerformerProviders.TryGetPerformersFor(context.Name, out var performer))
        {
            // No performer found, nothing to validate
            return Task.FromResult(queryResult);
        }

        var validationResults = new List<SystemValidationResult>();
        var queryArguments = context.Arguments ?? QueryArguments.Empty;
        DataAnnotationMembers.TryGet(context.Name, out var members);

        // Validate each parameter
        foreach (var parameter in performer.Parameters)
        {
            var member = members?.FirstOrDefault(member => member.Name == parameter.Name);
            var argumentWasProvided = queryArguments.TryGetValue(parameter.Name, out var value);

            // The performer marks non-nullable value parameters with declared defaults as optional.
            // Leave their omitted arguments to its default binding rather than validating a synthetic null.
            var usesDeclaredDefault = !argumentWasProvided && !parameter.IsRequired && !IsNullable(parameter.Type);
            if (!usesDeclaredDefault && (argumentWasProvided || member is not null))
            {
                ValidateParameter(parameter, value, validationResults, member);
            }
        }

        if (validationResults.Count > 0)
        {
            queryResult.ValidationResults = validationResults.Select(_ =>
                new CratisValidationResult(ValidationResultSeverity.Error, _.ErrorMessage ?? "Validation error", _.MemberNames, null!)).ToArray();
        }

        return Task.FromResult(queryResult);
    }

    static void ValidateParameter(QueryParameter parameter, object? value, List<SystemValidationResult> validationResults, DataAnnotationMember? member)
    {
        if (member is not null && !IsMissingNonNullableValue(parameter, value))
        {
            ValidateRegisteredParameter(parameter, member.ReadValue(value!), validationResults, member);
            return;
        }

        ValidateUnregisteredParameter(parameter, value, validationResults);
    }

    static void ValidateRegisteredParameter(QueryParameter parameter, object? value, List<SystemValidationResult> validationResults, DataAnnotationMember member)
    {
        var validationAttributes = member.Attributes();
        var validationContext = CreateValidationContext(parameter, value);
        validationContext.DisplayName = member.DisplayName;
        Validator.TryValidateValue(value, validationContext, validationResults, validationAttributes);
    }

    static void ValidateUnregisteredParameter(QueryParameter parameter, object? value, List<SystemValidationResult> validationResults)
    {
        if (IsMissingNonNullableValue(parameter, value))
        {
            validationResults.Add(new SystemValidationResult($"The field {parameter.Name} is required.", [parameter.Name]));
            return;
        }

        // Get validation attributes from the parameter type
        var validationContext = CreateValidationContext(parameter, value);
        foreach (var attribute in parameter.Type.GetCustomAttributes<ValidationAttribute>(true))
        {
            var result = attribute.GetValidationResult(value, validationContext);
            if (result != SystemValidationResult.Success)
            {
                validationResults.Add(result ?? new SystemValidationResult($"Validation failed for {parameter.Name}.", [parameter.Name]));
            }
        }
    }

    static ValidationContext CreateValidationContext(QueryParameter parameter, object? value) =>
        new(value ?? new object())
        {
            MemberName = parameter.Name
        };

    static bool IsMissingNonNullableValue(QueryParameter parameter, object? value) =>
        value is null && !IsNullable(parameter.Type);

    static bool IsNullable(Type type) =>
        !type.IsValueType || (Nullable.GetUnderlyingType(type) is not null);
}