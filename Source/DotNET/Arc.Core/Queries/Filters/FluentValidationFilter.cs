// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;

namespace Cratis.Arc.Queries.Filters;

/// <summary>
/// Represents a query filter that validates query parameters before they are performed.
/// </summary>
/// <param name="queryPerformerProviders">The <see cref="IQueryPerformerProviders"/> to use for finding query performers.</param>
/// <param name="queryArgumentsModels">The <see cref="IQueryArgumentsModels"/> to materialize a query's argument set with.</param>
/// <param name="modelGraphValidator">The <see cref="IModelGraphValidator"/> to validate with.</param>
/// <remarks>
/// Arguments are validated with the same traversal a command's properties go through, so a validator — a
/// <see cref="ConceptValidator{T}"/> in particular — behaves identically whether the value it guards arrives on a
/// command or as a query argument, however deeply it is nested.
/// </remarks>
public class FluentValidationFilter(
    IQueryPerformerProviders queryPerformerProviders,
    IQueryArgumentsModels queryArgumentsModels,
    IModelGraphValidator modelGraphValidator) : IQueryFilter
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

        // A query whose arguments are modelled as a whole is validated through that model: it covers every parameter,
        // and validating it as one graph reports members the way the client does — flat, from the argument set's own
        // perspective. Validating the arguments individually as well would report the same failures twice.
        queryResult.ValidationResults = queryArgumentsModels.TryCreateFor(performer, queryArguments, out var argumentsModel)
            ? await ValidateArgumentsModel(context, argumentsModel)
            : await ValidateArgumentsIndividually(context, performer, queryArguments);

        return queryResult;
    }

    async Task<IEnumerable<ValidationResult>> ValidateArgumentsModel(QueryContext context, object argumentsModel) =>
        await modelGraphValidator.Validate(new ModelGraphValidationRequest(argumentsModel, context.ServiceProvider));

    async Task<IEnumerable<ValidationResult>> ValidateArgumentsIndividually(QueryContext context, IQueryPerformer performer, QueryArguments queryArguments)
    {
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
                new ModelGraphValidationRequest(value, context.ServiceProvider, parameter.Name)));
        }

        return validationResults;
    }
}
