// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Arc.Commands;
using Cratis.Concepts;
using Cratis.Reflection;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Cratis.Arc.OpenApi;

/// <summary>
/// Represents an implementation of <see cref="IOpenApiOperationTransformer"/> that adds the command result to the operation for command methods.
/// </summary>
public class CommandResultOperationTransformer : IOpenApiOperationTransformer
{
    /// <inheritdoc/>
    public async Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        var methodInfo = context.GetMethodInfo();
        if (methodInfo?.IsCommand() != true)
        {
            return;
        }

        var returnType = methodInfo.GetActualReturnType();

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

        var schema = await context.GetOrCreateSchemaAsync(commandResultType, null, cancellationToken);
        var response = operation.Responses?.First().Value;
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
