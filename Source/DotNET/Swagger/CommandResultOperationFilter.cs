// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Arc.Commands;
using Cratis.Concepts;
using Cratis.Reflection;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Cratis.Arc.Swagger;

/// <summary>
/// Represents an implementation of <see cref="IOperationFilter"/> that adds the command result to the operation for command methods.
/// </summary>
public class CommandResultOperationFilter : IOperationFilter
{
    /// <inheritdoc/>
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (!context.MethodInfo.IsCommand()) return;

        var returnType = context.MethodInfo.GetActualReturnType();

        Type? commandResultType = null;

        if (returnType == typeof(Task) || returnType == typeof(ValueTask) || returnType == typeof(void))
        {
            returnType = typeof(object);
            commandResultType = typeof(CommandResult);
        }
        else if (returnType.IsConcept())
        {
            returnType = returnType.GetConceptValueType();
        }

        commandResultType ??= typeof(CommandResult<>).MakeGenericType(returnType);

        var schema = context.SchemaGenerator.GenerateSchema(commandResultType, context.SchemaRepository);
        var response = operation.Responses?.First().Value;
        MediaTypeUtilities.SetJsonSchema(response, schema);

        operation.Responses?.Add(((int)HttpStatusCode.Forbidden).ToString(), new OpenApiResponse()
        {
            Description = "Forbidden",
            Content = new Dictionary<string, IOpenApiMediaType>
            {
                { "application/json", new OpenApiMediaType() { Schema = schema } }
            }
        });

        operation.Responses?.Add(((int)HttpStatusCode.BadRequest).ToString(), new OpenApiResponse()
        {
            Description = "Bad Request - typically a validation error",
            Content = new Dictionary<string, IOpenApiMediaType>
            {
                { "application/json", new OpenApiMediaType() { Schema = schema } }
            }
        });

        operation.Responses?.Add(((int)HttpStatusCode.InternalServerError).ToString(), new OpenApiResponse()
        {
            Description = "Internal server error - something went wrong. See the exception details.",
            Content = new Dictionary<string, IOpenApiMediaType>
            {
                { "application/json", new OpenApiMediaType() { Schema = schema } }
            }
        });
    }
}
