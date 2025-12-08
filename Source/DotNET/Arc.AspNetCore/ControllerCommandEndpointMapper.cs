// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Arc.Execution;
using Cratis.Execution;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing;

namespace Cratis.Arc.Commands;

/// <summary>
/// Maps validation endpoints for controller-based commands.
/// </summary>
/// <param name="actionDescriptorCollectionProvider">Provider for discovering controller actions.</param>
/// <param name="commandPipeline">The command pipeline for executing validation.</param>
/// <param name="correlationIdAccessor">Accessor for correlation IDs.</param>
/// <param name="jsonSerializerOptions">JSON serialization options.</param>
public class ControllerCommandEndpointMapper(
    IActionDescriptorCollectionProvider actionDescriptorCollectionProvider,
    ICommandPipeline commandPipeline,
    ICorrelationIdAccessor correlationIdAccessor,
    JsonSerializerOptions jsonSerializerOptions)
{
    static readonly string[] _postHttpMethod = ["POST"];

    /// <summary>
    /// Maps validation endpoints for all controller-based commands.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="arcOptions">Arc options.</param>
    public void MapValidationEndpoints(IEndpointRouteBuilder endpoints, ArcOptions arcOptions)
    {
        foreach (var action in GetControllerCommandActions())
        {
            var route = BuildValidationRoute(action);
            var commandType = GetCommandType(action);

            if (commandType is null) continue;

            var endpointName = $"Validate{action.ControllerName}{action.ActionName}";
            if (endpoints.EndpointExists(endpointName))
            {
                continue;
            }

            endpoints.Map(route, async context =>
            {
                context.HandleCorrelationId(correlationIdAccessor, arcOptions.CorrelationId);

                var command = await context.Request.ReadFromJsonAsync(commandType, jsonSerializerOptions, cancellationToken: context.RequestAborted);
                CommandResult commandResult;

                if (command is null)
                {
                    commandResult = CommandResult.Error(correlationIdAccessor.Current, $"Could not deserialize command of type '{commandType}' from request body.");
                }
                else
                {
                    commandResult = await commandPipeline.Validate(command);
                }

                context.Response.SetResponseStatusCode(commandResult);
                await context.Response.WriteAsJsonAsync(commandResult, commandResult.GetType(), jsonSerializerOptions, cancellationToken: context.RequestAborted);
            })
            .WithMetadata(new HttpMethodMetadata(_postHttpMethod))
            .WithTags(action.ControllerName)
            .WithName(endpointName)
            .WithSummary($"Validate {action.ActionName} command without executing it");
        }
    }

    IEnumerable<ControllerActionDescriptor> GetControllerCommandActions()
    {
        return actionDescriptorCollectionProvider.ActionDescriptors.Items
            .OfType<ControllerActionDescriptor>()
            .Where(IsCommandAction);
    }

    bool IsCommandAction(ControllerActionDescriptor action)
    {
        // Command actions are POST methods with a single [FromBody] parameter
        if (!action.ActionName.Equals("POST", StringComparison.OrdinalIgnoreCase) &&
            !action.EndpointMetadata.Any(m => m is HttpPostAttribute))
        {
            return false;
        }

        // Look for a single FromBody parameter (the command)
        var fromBodyParameters = action.Parameters
            .Where(p => p.BindingInfo?.BindingSource == Microsoft.AspNetCore.Mvc.ModelBinding.BindingSource.Body)
            .ToList();

        return fromBodyParameters.Count == 1;
    }

    Type? GetCommandType(ControllerActionDescriptor action)
    {
        var fromBodyParameter = action.Parameters
            .FirstOrDefault(p => p.BindingInfo?.BindingSource == Microsoft.AspNetCore.Mvc.ModelBinding.BindingSource.Body);

        return fromBodyParameter?.ParameterType;
    }

    string BuildValidationRoute(ControllerActionDescriptor action)
    {
        // Get the route template from the action
        var routeTemplate = action.AttributeRouteInfo?.Template ?? string.Empty;

        // Append /validate to the route
        return $"{routeTemplate}/validate";
    }
}
