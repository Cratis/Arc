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

        // Validate each parameter
        foreach (var parameter in performer.Parameters)
        {
            if (queryArguments.TryGetValue(parameter.Name, out var value))
            {
                ValidateParameter(parameter, value, validationResults);
            }
        }

        if (validationResults.Count > 0)
        {
            queryResult.ValidationResults = validationResults.Select(_ =>
                new CratisValidationResult(ValidationResultSeverity.Error, _.ErrorMessage ?? "Validation error", _.MemberNames, null!)).ToArray();
        }

        return Task.FromResult(queryResult);
    }

    static void ValidateParameter(QueryParameter parameter, object? value, List<SystemValidationResult> validationResults)
    {
        if (value is null && !IsNullable(parameter.Type))
        {
            validationResults.Add(new SystemValidationResult($"The field {parameter.Name} is required.", [parameter.Name]));
            return;
        }

        // Get validation attributes from the parameter type
        var validationAttributes = parameter.Type.GetCustomAttributes<ValidationAttribute>(true);
        var validationContext = new ValidationContext(value ?? new object())
        {
            MemberName = parameter.Name
        };

        foreach (var attribute in validationAttributes)
        {
            var result = attribute.GetValidationResult(value, validationContext);
            if (result != SystemValidationResult.Success)
            {
                validationResults.Add(result ?? new SystemValidationResult($"Validation failed for {parameter.Name}.", [parameter.Name]));
            }
        }
    }

    static bool IsNullable(Type type) =>
        !type.IsValueType || (Nullable.GetUnderlyingType(type) is not null);
}