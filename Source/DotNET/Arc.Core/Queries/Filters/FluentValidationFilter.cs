// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Validation;
using FluentValidation;

namespace Cratis.Arc.Queries.Filters;

/// <summary>
/// Represents a query filter that validates query parameters before they are performed.
/// </summary>
/// <param name="queryPerformerProviders">The <see cref="IQueryPerformerProviders"/> to use for finding query performers.</param>
/// <param name="discoverableValidators">The <see cref="IDiscoverableValidators"/> to use for finding validators.</param>
public class FluentValidationFilter(IQueryPerformerProviders queryPerformerProviders, IDiscoverableValidators discoverableValidators) : IQueryFilter
{
    /// <inheritdoc/>
    public async Task<QueryResult> OnPerform(QueryContext context)
    {
        var queryResult = QueryResult.Success(context.CorrelationId);

        if (!queryPerformerProviders.TryGetPerformersFor(context.Name, out var performer))
        {
            return queryResult;
        }

        var queryArguments = context.Arguments ?? QueryArguments.Empty;

        foreach (var parameter in performer.Parameters)
        {
            if (queryArguments.TryGetValue(parameter.Name, out var value))
            {
                var validationResult = await ValidateParameter(parameter, value);
                queryResult.MergeWith(validationResult);
            }
        }

        return queryResult;
    }

    static ValidationResultSeverity MapSeverity(Severity severity) => severity switch
    {
        Severity.Info => ValidationResultSeverity.Information,
        Severity.Warning => ValidationResultSeverity.Warning,
        Severity.Error => ValidationResultSeverity.Error,
        _ => ValidationResultSeverity.Error
    };

    async Task<QueryResult> ValidateParameter(QueryParameter parameter, object? value)
    {
        var queryResult = QueryResult.Success(Cratis.Execution.CorrelationId.NotSet);

        if (value is null)
        {
            return queryResult;
        }

        var parameterType = parameter.Type;
        if (discoverableValidators.TryGet(parameterType, out var validator))
        {
            var validationContextType = typeof(ValidationContext<>).MakeGenericType(parameterType);
            var validationContext = Activator.CreateInstance(validationContextType, value) as IValidationContext;
            var validationResult = await validator.ValidateAsync(validationContext);
            if (!validationResult.IsValid)
            {
                queryResult.MergeWith(new QueryResult
                {
                    ValidationResults = validationResult.Errors.Select(_ =>
                        new ValidationResult(MapSeverity(_.Severity), _.ErrorMessage, [_.PropertyName], null!)).ToArray()
                });
            }

            if (!parameterType.IsPrimitive)
            {
                foreach (var property in parameterType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    var propertyValue = property.GetValue(value);
                    if (propertyValue is not null)
                    {
                        var propertyParameter = new QueryParameter(property.Name, property.PropertyType);
                        var propertyValidationResult = await ValidateParameter(propertyParameter, propertyValue);
                        queryResult.MergeWith(propertyValidationResult);
                    }
                }
            }
        }

        return queryResult;
    }
}