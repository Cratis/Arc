// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Http.Discovery;

/// <summary>
/// Maps endpoint discovery routes.
/// </summary>
public static class DiscoveryEndpointMapper
{
    const string CommandsEndpointName = "DiscoverCommands";
    const string QueriesEndpointName = "DiscoverQueries";

    /// <summary>
    /// Maps discovery endpoints for commands and queries.
    /// </summary>
    /// <param name="mapper">The <see cref="IEndpointMapper"/> to use.</param>
    public static void MapDiscoveryEndpoints(this IEndpointMapper mapper)
    {
        if (!mapper.EndpointExists(CommandsEndpointName))
        {
            mapper.MapGet(
                "/.cratis/commands",
                async context =>
                {
                    var discoveryService = context.RequestServices.GetRequiredService<IDiscoveryService>();
                    var (commands, _) = discoveryService.DiscoverAllEndpoints();
                    await context.WriteResponseAsJson(commands, typeof(List<CommandDiscoveryMetadata>), context.RequestAborted);
                },
                new EndpointMetadata(
                    CommandsEndpointName,
                    "Discover available command endpoints",
                    ["Cratis Discovery"],
                    AllowAnonymous: true,
                    ResponseType: typeof(List<CommandDiscoveryMetadata>)));
        }

        if (!mapper.EndpointExists(QueriesEndpointName))
        {
            mapper.MapGet(
                "/.cratis/queries",
                async context =>
                {
                    var discoveryService = context.RequestServices.GetRequiredService<IDiscoveryService>();
                    var (_, queries) = discoveryService.DiscoverAllEndpoints();
                    await context.WriteResponseAsJson(queries, typeof(List<QueryDiscoveryMetadata>), context.RequestAborted);
                },
                new EndpointMetadata(
                    QueriesEndpointName,
                    "Discover available query endpoints",
                    ["Cratis Discovery"],
                    AllowAnonymous: true,
                    ResponseType: typeof(List<QueryDiscoveryMetadata>)));
        }
    }
}
