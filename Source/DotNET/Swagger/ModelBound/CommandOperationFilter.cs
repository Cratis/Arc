// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Arc.Commands;
using Cratis.Concepts;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Cratis.Arc.Swagger.ModelBound;

/// <summary>
/// Represents an implementation of <see cref="IOperationFilter"/> that adds command payload and result documentation for model-bound minimal APIs.
/// </summary>
/// <param name="commandHandlerProviders">The command handler providers.</param>
public class CommandOperationFilter(ICommandHandlerProviders commandHandlerProviders) : IOperationFilter
{
    /// <inheritdoc/>
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        // Check if this operation corresponds to a command endpoint by matching the route pattern
        var commandHandler = FindMatchingCommandHandler(operation, commandHandlerProviders);
        if (commandHandler is null) return;

        // Set up request body schema for the command payload
        SetupCommandRequestBody(operation, commandHandler, context);

        // Set up response schemas for the command result
        SetupCommandResponseSchemas(operation, commandHandler, context);
    }

    static ICommandHandler? FindMatchingCommandHandler(OpenApiOperation operation, ICommandHandlerProviders commandHandlerProviders)
    {
        var operationId = operation.OperationId;
        if (string.IsNullOrEmpty(operationId) || !operationId.StartsWith("Execute"))
        {
            return null;
        }

        var commandTypeName = operationId.Substring("Execute".Length);
        return commandHandlerProviders.Handlers.FirstOrDefault(h => h.CommandType.FullName == commandTypeName);
    }

    static void SetupCommandRequestBody(OpenApiOperation operation, ICommandHandler commandHandler, OperationFilterContext context)
    {
        var commandType = commandHandler.CommandType;
        var schema = context.SchemaGenerator.GenerateSchema(commandType, context.SchemaRepository);

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

    static void SetupCommandResponseSchemas(OpenApiOperation operation, ICommandHandler commandHandler, OperationFilterContext context)
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

        var schema = context.SchemaGenerator.GenerateSchema(commandResultType, context.SchemaRepository);

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
