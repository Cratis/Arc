// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Arc;
using Cratis.Arc.Commands;
using Cratis.Arc.Execution;
using Cratis.Execution;
using Cratis.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Provides extension methods for adding command endpoints.
/// </summary>
public static class CommandEndpointsExtensions
{
    /// <summary>
    /// Use Cratis default setup.
    /// </summary>
    /// <param name="app"><see cref="IApplicationBuilder"/> to extend.</param>
    /// <returns><see cref="IApplicationBuilder"/> for continuation.</returns>
    public static IApplicationBuilder UseCommandEndpoints(this IApplicationBuilder app)
    {
        if (app is IEndpointRouteBuilder endpoints)
        {
            var arcOptions = app.ApplicationServices.GetRequiredService<IOptions<ArcOptions>>().Value;
            var options = arcOptions.GeneratedApis;
            var correlationIdAccessor = app.ApplicationServices.GetRequiredService<ICorrelationIdAccessor>();
            var commandPipeline = app.ApplicationServices.GetRequiredService<ICommandPipeline>();
            var commandHandlerProviders = app.ApplicationServices.GetRequiredService<ICommandHandlerProviders>();
            var jsonSerializerOptions = Globals.JsonSerializerOptions;

            var prefix = options.RoutePrefix.Trim('/');
            var group = endpoints.MapGroup($"/{prefix}");

            // Map model-bound command endpoints
            foreach (var handler in commandHandlerProviders.Handlers)
            {
                var location = handler.Location.Skip(options.SegmentsToSkipForRoute);
                var segments = location.Select(segment => segment.ToKebabCase());
                var baseUrl = $"/{string.Join('/', segments)}";
                var typeName = options.IncludeCommandNameInRoute ? handler.CommandType.Name : string.Empty;

                var url = options.IncludeCommandNameInRoute ? $"{baseUrl}/{typeName.ToKebabCase()}" : baseUrl;
                url = url.ToLowerInvariant();

                MapCommandEndpoint(
                    group,
                    endpoints,
                    url,
                    $"Execute{handler.CommandType.FullName}",
                    $"Execute {handler.CommandType.Name} command in {handler.CommandType.Namespace}",
                    handler.CommandType,
                    location,
                    handler.AllowsAnonymousAccess,
                    correlationIdAccessor,
                    arcOptions,
                    jsonSerializerOptions,
                    commandPipeline.Execute);

                MapCommandEndpoint(
                    group,
                    endpoints,
                    $"{url}/validate",
                    $"Validate{handler.CommandType.Name}",
                    $"Validate {handler.CommandType.Name} command without executing it",
                    handler.CommandType,
                    location,
                    handler.AllowsAnonymousAccess,
                    correlationIdAccessor,
                    arcOptions,
                    jsonSerializerOptions,
                    commandPipeline.Validate);
            }

            // Map controller-based command validation endpoints
            var actionDescriptorProvider = app.ApplicationServices.GetRequiredService<IActionDescriptorCollectionProvider>();
            var controllerCommandMapper = new ControllerCommandEndpointMapper(
                actionDescriptorProvider,
                commandPipeline,
                correlationIdAccessor,
                jsonSerializerOptions);
            controllerCommandMapper.MapValidationEndpoints(endpoints, arcOptions);
        }

        return app;
    }

    static void MapCommandEndpoint(
        RouteGroupBuilder group,
        IEndpointRouteBuilder endpoints,
        string url,
        string endpointName,
        string summary,
        Type commandType,
        IEnumerable<string> location,
        bool allowAnonymous,
        ICorrelationIdAccessor correlationIdAccessor,
        ArcOptions arcOptions,
        JsonSerializerOptions jsonSerializerOptions,
        Func<object, Task<CommandResult>> commandOperation)
    {
        if (endpoints.EndpointExists(endpointName))
        {
            return;
        }

        // Note: If we use the minimal API "MapPost" with HttpContext parameter, it does not show up in Swagger
        //       So we use HttpRequest and HttpResponse instead
        var builder = group.MapPost(url, async (HttpRequest request, HttpResponse response) =>
        {
            var context = request.HttpContext;
            context.HandleCorrelationId(correlationIdAccessor, arcOptions.CorrelationId);
            var command = await request.ReadFromJsonAsync(commandType, jsonSerializerOptions, cancellationToken: context.RequestAborted);
            CommandResult commandResult;
            if (command is null)
            {
                commandResult = CommandResult.Error(correlationIdAccessor.Current, $"Could not deserialize command of type '{commandType}' from request body.");
            }
            else
            {
                commandResult = await commandOperation(command);
            }
            response.SetResponseStatusCode(commandResult);
            await response.WriteAsJsonAsync(commandResult, commandResult.GetType(), jsonSerializerOptions, cancellationToken: context.RequestAborted);
        })
        .WithTags(string.Join('.', location))
        .WithName(endpointName)
        .WithSummary(summary);

        if (allowAnonymous)
        {
            builder.AllowAnonymous();
        }
    }
}
