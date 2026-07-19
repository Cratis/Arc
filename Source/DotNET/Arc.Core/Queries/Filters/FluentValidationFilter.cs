// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;

namespace Cratis.Arc.Queries.Filters;

/// <summary>
/// Represents a query filter that validates query parameters before they are performed.
/// </summary>
/// <param name="queryPerformerProviders">The <see cref="IQueryPerformerProviders"/> to use for finding query performers.</param>
/// <param name="modelGraphValidator">The <see cref="IModelGraphValidator"/> to validate each argument graph with.</param>
/// <remarks>
/// Each argument is validated with the same traversal a command's properties go through, so a validator — a
/// <see cref="ConceptValidator{T}"/> in particular — behaves identically whether the value it guards arrives on a
/// command or as a query argument, however deeply it is nested.
/// </remarks>
public class FluentValidationFilter(IQueryPerformerProviders queryPerformerProviders, IModelGraphValidator modelGraphValidator) : IQueryFilter
{
    /// <summary>
    /// The message surfaced when a validator throws while validating a query argument.
    /// </summary>
    internal const string MessageWhenValidatorThrows = "The query could not be validated.";

    /// <inheritdoc/>
    public async Task<QueryResult> OnPerform(QueryContext context)
    {
        var queryResult = QueryResult.Success(context.CorrelationId);

        if (!queryPerformerProviders.TryGetPerformersFor(context.Name, out var performer))
        {
            return queryResult;
        }

        var queryArguments = context.Arguments ?? QueryArguments.Empty;
        var validationResults = new List<ValidationResult>();

        foreach (var parameter in performer.Parameters)
        {
            // A missing or null argument is not this filter's concern — required arguments are enforced by the
            // performer, and a validator cannot run against a value that is not there.
            if (!queryArguments.TryGetValue(parameter.Name, out var value) || value is null)
            {
                continue;
            }

            validationResults.AddRange(await modelGraphValidator.Validate(
                new ModelGraphValidationRequest(
                    value,
                    context.ServiceProvider,
                    parameter.Name,
                    MessageWhenValidatorThrows)));
        }

        queryResult.ValidationResults = validationResults;
        return queryResult;
    }
}
