// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Arc.Commands;
using Cratis.Concepts;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Cratis.Arc.OpenApi.ModelBound;

/// <summary>
/// Represents an implementation of <see cref="IOpenApiOperationTransformer"/> that adds command payload and result documentation for model-bound minimal APIs.
/// </summary>
/// <param name="commandHandlerProviders">The command handler providers.</param>
public class CommandOperationTransformer(ICommandHandlerProviders commandHandlerProviders) : IOpenApiOperationTransformer
{
    /// <inheritdoc/>
    public async Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        var commandHandler = FindMatchingCommandHandler(operation, commandHandlerProviders);
        if (commandHandler is null) return;

        await SetupCommandRequestBody(operation, commandHandler, context, cancellationToken);

        await SetupCommandResponseSchemas(operation, commandHandler, context, cancellationToken);
    }

    static ICommandHandler? FindMatchingCommandHandler(OpenApiOperation operation, ICommandHandlerProviders commandHandlerProviders)
    {
        var operationId = operation.OperationId;
        if (string.IsNullOrEmpty(operationId) || !operationId.StartsWith("Execute"))
        {
            return null;
        }

        var commandTypeName = operationId.Substring("Execute".Length);
        return commandHandlerProviders.Handlers.FirstOrDefault(h => h.CommandType.Name == commandTypeName);
    }

    static async Task SetupCommandRequestBody(OpenApiOperation operation, ICommandHandler commandHandler, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        var commandType = commandHandler.CommandType;
        var schema = await context.GetOrCreateSchemaAsync(commandType, null, cancellationToken);

        operation.RequestBody = new OpenApiRequestBody
        {
            Description = $"The {commandType.Name} command payload",
            Required = true,
            Content = new Dictionary<string, OpenApiMediaType>
            {
                { "application/json", new OpenApiMediaType { Schema = schema } }
            }
        };
    }

    static async Task SetupCommandResponseSchemas(OpenApiOperation operation, ICommandHandler commandHandler, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        var handleMethod = commandHandler.GetType().GetMethod("Handle");
        var returnType = handleMethod?.ReturnType;

        Type? commandResultType = null;
        Type actualReturnType;

        if (returnType is not null)
        {
            if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
            {
                actualReturnType = returnType.GetGenericArguments()[0];
            }
            else if (returnType == typeof(Task) || returnType == typeof(ValueTask) || returnType == typeof(void))
            {
                actualReturnType = typeof(object);
            }
            else
            {
                actualReturnType = returnType;
            }

            if (actualReturnType == typeof(Task) || actualReturnType == typeof(ValueTask) || actualReturnType == typeof(void) || actualReturnType == typeof(object))
            {
                commandResultType = typeof(CommandResult);
            }
            else
            {
                if (actualReturnType.IsConcept())
                {
                    actualReturnType = actualReturnType.GetConceptValueType();
                }
                commandResultType = typeof(CommandResult<>).MakeGenericType(actualReturnType);
            }
        }

        commandResultType ??= typeof(CommandResult);

        var schema = await context.GetOrCreateSchemaAsync(commandResultType, null, cancellationToken);

        if (operation.Responses?.TryGetValue("200", out var okResponse) == true)
        {
            okResponse.Description = "Command executed successfully";
            okResponse.Content?.Clear();
            okResponse.Content?.Add("application/json", new OpenApiMediaType { Schema = schema });
        }
        else
        {
            operation.Responses?.Add("200", new OpenApiResponse
            {
                Description = "Command executed successfully",
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    { "application/json", new OpenApiMediaType { Schema = schema } }
                }
            });
        }

        operation.Responses?.Add(((int)HttpStatusCode.BadRequest).ToString(), new OpenApiResponse
        {
            Description = "Bad Request - validation error or malformed payload",
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
