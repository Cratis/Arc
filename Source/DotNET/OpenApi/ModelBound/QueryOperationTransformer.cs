// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Arc.Queries;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Cratis.Arc.OpenApi.ModelBound;

/// <summary>
/// Represents an implementation of <see cref="IOpenApiOperationTransformer"/> that adds query arguments and result documentation for model-bound minimal APIs.
/// </summary>
/// <param name="queryPerformerProviders">The query performer providers.</param>
public class QueryOperationTransformer(IQueryPerformerProviders queryPerformerProviders) : IOpenApiOperationTransformer
{
    /// <inheritdoc/>
    public async Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        var queryPerformer = FindMatchingQueryPerformer(operation, queryPerformerProviders);
        if (queryPerformer is null) return;

        await SetupQueryParameters(operation, queryPerformer, context, cancellationToken);

        await SetupQueryResponseSchemas(operation, context, cancellationToken);
    }

    static IQueryPerformer? FindMatchingQueryPerformer(OpenApiOperation operation, IQueryPerformerProviders queryPerformerProviders)
    {
        var operationId = operation.OperationId;
        if (string.IsNullOrEmpty(operationId) || !operationId.StartsWith("Execute"))
        {
            return null;
        }

        var queryTypeName = operationId.Substring("Execute".Length);
        return queryPerformerProviders.Performers.FirstOrDefault(p => p.Name.ToString() == queryTypeName);
    }

    static async Task SetupQueryParameters(OpenApiOperation operation, IQueryPerformer queryPerformer, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        foreach (var parameter in queryPerformer.Parameters)
        {
            var parameterSchema = await context.GetOrCreateSchemaAsync(parameter.Type, null, cancellationToken);

            operation.Parameters?.Add(new OpenApiParameter
            {
                Name = parameter.Name,
                In = ParameterLocation.Query,
                Description = $"Query parameter: {parameter.Name}",
                Required = false,
                Schema = parameterSchema
            });
        }

        var performMethod = queryPerformer.GetType().GetMethod("Perform");
        var returnType = performMethod?.ReturnType;

        if (returnType is not null && IsEnumerableResult(returnType))
        {
            QueryParameterUtilities.AddPagingAndSortingParameters(operation);
        }
    }

    static async Task SetupQueryResponseSchemas(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        var queryResultType = typeof(QueryResult);
        var schema = await context.GetOrCreateSchemaAsync(queryResultType, null, cancellationToken);

        if (operation.Responses?.TryGetValue("200", out var okResponse) == true)
        {
            okResponse.Description = "Query executed successfully";
            okResponse.Content?.Clear();
            okResponse.Content?.Add("application/json", new OpenApiMediaType { Schema = schema });
        }
        else
        {
            operation.Responses?.Add("200", new OpenApiResponse
            {
                Description = "Query executed successfully",
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    { "application/json", new OpenApiMediaType { Schema = schema } }
                }
            });
        }

        operation.Responses?.Add(((int)HttpStatusCode.BadRequest).ToString(), new OpenApiResponse
        {
            Description = "Bad Request - validation error or invalid parameters",
            Content = new Dictionary<string, OpenApiMediaType>
            {
                { "application/json", new OpenApiMediaType { Schema = schema } }
            }
        });

        operation.Responses?.Add(((int)HttpStatusCode.Forbidden).ToString(), new OpenApiResponse
        {
            Description = "Forbidden - insufficient permissions",
            Content = new Dictionary<string, OpenApiMediaType>
            {
                { "application/json", new OpenApiMediaType { Schema = schema } }
            }
        });

        operation.Responses?.Add(((int)HttpStatusCode.InternalServerError).ToString(), new OpenApiResponse
        {
            Description = "Internal server error",
            Content = new Dictionary<string, OpenApiMediaType>
            {
                { "application/json", new OpenApiMediaType { Schema = schema } }
            }
        });
    }

    static bool IsEnumerableResult(Type returnType)
    {
        if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            returnType = returnType.GetGenericArguments()[0];
        }

        return returnType != typeof(string) &&
               returnType.GetInterfaces().Any(i =>
                   i.IsGenericType &&
                   i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
    }
}
