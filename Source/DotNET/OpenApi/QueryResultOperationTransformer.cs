// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Arc.Queries;
using Cratis.Concepts;
using Cratis.Reflection;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Cratis.Arc.OpenApi;

/// <summary>
/// Represents an implementation of <see cref="IOpenApiOperationTransformer"/> that adds the query result to the operation for query methods.
/// </summary>
public class QueryResultOperationTransformer : IOpenApiOperationTransformer
{
    /// <inheritdoc/>
    public async Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        var methodInfo = context.GetMethodInfo();
        if (methodInfo?.IsQuery() != true)
        {
            return;
        }

        var returnType = methodInfo.GetActualReturnType();
        if (returnType.IsConcept())
        {
            returnType = returnType.GetConceptValueType();
        }

        var queryResultType = typeof(QueryResult);

        if (methodInfo.ReturnType.IsEnumerable())
        {
            QueryParameterUtilities.AddPagingAndSortingParameters(operation);
        }

        var schema = await context.GetOrCreateSchemaAsync(queryResultType, null, cancellationToken);
        var response = operation.Responses?.FirstOrDefault((kvp) => kvp.Key == ((int)HttpStatusCode.OK).ToString()).Value;
        if (response?.Content?.TryGetValue("application/json", out var value) == true)
        {
            value.Schema = schema;
        }
        else
        {
            response?.Content?.Add(new("application/json", new() { Schema = schema }));
        }

        operation.Responses?.Add(((int)HttpStatusCode.Forbidden).ToString(), new OpenApiResponse()
        {
            Description = "Forbidden",
            Content = new Dictionary<string, OpenApiMediaType>
            {
                { "application/json", new OpenApiMediaType() { Schema = schema } }
            }
        });

        operation.Responses?.Add(((int)HttpStatusCode.BadRequest).ToString(), new OpenApiResponse()
        {
            Description = "Bad Request - typically a validation error",
            Content = new Dictionary<string, OpenApiMediaType>
            {
                { "application/json", new OpenApiMediaType() { Schema = schema } }
            }
        });

        operation.Responses?.Add(((int)HttpStatusCode.InternalServerError).ToString(), new OpenApiResponse()
        {
            Description = "Internal server error - something went wrong. See the exception details.",
            Content = new Dictionary<string, OpenApiMediaType>
            {
                { "application/json", new OpenApiMediaType() { Schema = schema } }
            }
        });
    }
}
