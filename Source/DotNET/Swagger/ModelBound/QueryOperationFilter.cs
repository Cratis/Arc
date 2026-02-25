// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Arc.Queries;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Cratis.Arc.Swagger.ModelBound;

/// <summary>
/// Represents an implementation of <see cref="IOperationFilter"/> that adds query arguments and result documentation for model-bound minimal APIs.
/// </summary>
/// <param name="queryPerformerProviders">The query performer providers.</param>
public class QueryOperationFilter(IQueryPerformerProviders queryPerformerProviders) : IOperationFilter
{
    /// <inheritdoc/>
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        // Check if this operation corresponds to a query endpoint by matching the route pattern
        var queryPerformer = FindMatchingQueryPerformer(operation, queryPerformerProviders);
        if (queryPerformer is null) return;

        // Set up query parameters
        SetupQueryParameters(operation, queryPerformer, context);

        // Set up response schemas for the query result
        SetupQueryResponseSchemas(operation, context);
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

    static void SetupQueryParameters(OpenApiOperation operation, IQueryPerformer queryPerformer, OperationFilterContext context)
    {
        // Add parameters from the query performer's parameters
        foreach (var parameter in queryPerformer.Parameters)
        {
            var parameterSchema = context.SchemaGenerator.GenerateSchema(parameter.Type, context.SchemaRepository);

            operation.Parameters?.Add(new OpenApiParameter
            {
                Name = parameter.Name,
                In = ParameterLocation.Query,
                Description = $"Query parameter: {parameter.Name}",
                Required = false, // Query parameters are typically optional
                Schema = parameterSchema
            });
        }

        // Add standard paging and sorting parameters for enumerable results
        if (queryPerformer.IsEnumerableResult)
        {
            QueryParameterUtilities.AddPagingAndSortingParameters(operation);
        }
    }

    static void SetupQueryResponseSchemas(OpenApiOperation operation, OperationFilterContext context)
    {
        // Always use QueryResult as the wrapper for query responses
        var queryResultType = typeof(QueryResult);
        var schema = context.SchemaGenerator.GenerateSchema(queryResultType, context.SchemaRepository);

        // Update the 200 OK response
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

        // Add additional error response codes
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
}
