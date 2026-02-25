// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;
using Cratis.Execution;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Commands;

/// <summary>
/// Maps command endpoints using the provided endpoint mapper.
/// </summary>
public static class CommandEndpointMapper
{
    /// <summary>
    /// Maps all command endpoints.
    /// </summary>
    /// <param name="mapper">The <see cref="IEndpointMapper"/> to use.</param>
    /// <param name="serviceProvider">The <see cref="IServiceProvider"/>.</param>
    public static void MapCommandEndpoints(this IEndpointMapper mapper, IServiceProvider serviceProvider)
    {
        var arcOptions = serviceProvider.GetRequiredService<IOptions<ArcOptions>>().Value;
        var options = arcOptions.GeneratedApis;
        var commandHandlerProviders = serviceProvider.GetRequiredService<ICommandHandlerProviders>();

        var handlersByNamespace = EndpointRouteHelper.GroupByNamespace(
            commandHandlerProviders.Handlers,
            h => h.Location,
            options.SegmentsToSkipForRoute);

        foreach (var handler in commandHandlerProviders.Handlers)
        {
            var location = handler.Location.Skip(options.SegmentsToSkipForRoute);
            var includeCommandName = EndpointRouteHelper.ShouldIncludeNameInRoute(
                options.IncludeCommandNameInRoute,
                location,
                handlersByNamespace);
            var url = EndpointRouteHelper.BuildRouteUrl(options, location, handler.CommandType.Name, includeCommandName);

            MapCommandEndpoint(
                mapper,
                url,
                $"Execute{handler.CommandType.FullName}",
                $"Execute {handler.CommandType.Name} command in {handler.CommandType.Namespace}",
                handler.CommandType,
                location,
                handler.AllowsAnonymousAccess);

            MapCommandEndpoint(
                mapper,
                $"{url}/validate",
                $"Validate{handler.CommandType.FullName}",
                $"Validate {handler.CommandType.Name} command without executing it",
                handler.CommandType,
                location,
                handler.AllowsAnonymousAccess,
                validateOnly: true);
        }
    }

    static void MapCommandEndpoint(
        IEndpointMapper mapper,
        string url,
        string endpointName,
        string summary,
        Type commandType,
        IEnumerable<string> location,
        bool allowAnonymous,
        bool validateOnly = false)
    {
        if (mapper.EndpointExists(endpointName))
        {
            return;
        }

        var metadata = new EndpointMetadata(
            endpointName,
            summary,
            [string.Join('.', location)],
            allowAnonymous,
            RequestBodyType: commandType,
            ResponseType: typeof(CommandResult));

        mapper.MapPost(
            url,
            async context =>
            {
                var correlationIdAccessor = context.RequestServices.GetRequiredService<ICorrelationIdAccessor>();
                var commandPipeline = context.RequestServices.GetRequiredService<ICommandPipeline>();
                var arcOptions = context.RequestServices.GetRequiredService<IOptions<ArcOptions>>().Value;

                context.HandleCorrelationId(correlationIdAccessor, arcOptions.CorrelationId);

                CommandResult commandResult;
                try
                {
                    var command = await context.ReadBodyAsJson(commandType, context.RequestAborted);

                    if (command is null)
                    {
                        commandResult = CommandResult.Error(correlationIdAccessor.Current, $"Could not deserialize command of type '{commandType}' from request body.");
                    }
                    else
                    {
                        commandResult = validateOnly
                            ? await commandPipeline.Validate(command, context.RequestServices)
                            : await commandPipeline.Execute(command, context.RequestServices);
                    }
                }
                catch (Exception ex)
                {
                    commandResult = CommandResult.Error(correlationIdAccessor.Current, $"Failed to read request body: {ex.Message}");
                }

                var statusCode = EndpointRouteHelper.GetStatusCode(commandResult.IsSuccess, commandResult.IsAuthorized, commandResult.IsValid);
                context.SetStatusCode(statusCode);
                await context.WriteResponseAsJson(commandResult, commandResult.GetType(), context.RequestAborted);
            },
            metadata);
    }
}
