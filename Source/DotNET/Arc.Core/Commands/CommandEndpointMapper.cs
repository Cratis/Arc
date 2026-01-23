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

        var prefix = options.RoutePrefix.Trim('/');

        var handlersByNamespace = commandHandlerProviders.Handlers
            .GroupBy(h => string.Join('.', h.Location.Skip(options.SegmentsToSkipForRoute)))
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var handler in commandHandlerProviders.Handlers)
        {
            var location = handler.Location.Skip(options.SegmentsToSkipForRoute);
            var segments = location.Select(segment => segment.ToKebabCase());
            var baseUrl = $"/{prefix}/{string.Join('/', segments)}";

            var namespaceKey = string.Join('.', location);
            var hasConflict = handlersByNamespace.TryGetValue(namespaceKey, out var handlersInNamespace) && handlersInNamespace.Count > 1;
            var includeCommandName = options.IncludeCommandNameInRoute || hasConflict;
            var typeName = includeCommandName ? handler.CommandType.Name : string.Empty;

            var url = includeCommandName ? $"{baseUrl}/{typeName.ToKebabCase()}" : baseUrl;
            url = url.ToLowerInvariant();

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
            allowAnonymous);

        mapper.MapPost(
            url,
            async context =>
            {
                var correlationIdAccessor = context.RequestServices.GetRequiredService<ICorrelationIdAccessor>();
                var commandPipeline = context.RequestServices.GetRequiredService<ICommandPipeline>();
                var arcOptions = context.RequestServices.GetRequiredService<IOptions<ArcOptions>>().Value;

                context.HandleCorrelationId(correlationIdAccessor, arcOptions.CorrelationId);

                var command = await context.ReadBodyAsJsonAsync(commandType, context.RequestAborted);
                CommandResult commandResult;

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

                context.SetStatusCode(commandResult.IsSuccess ? 200 : !commandResult.IsValid ? 400 : 500);
                await context.WriteResponseAsJsonAsync(commandResult, commandResult.GetType(), context.RequestAborted);
            },
            metadata);
    }
}
