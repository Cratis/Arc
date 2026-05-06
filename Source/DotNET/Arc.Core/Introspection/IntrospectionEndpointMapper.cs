// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Introspection;

/// <summary>
/// Maps endpoint introspection routes.
/// </summary>
public static class IntrospectionEndpointMapper
{
    const string CommandsEndpointName = "IntrospectCommands";
    const string QueriesEndpointName = "IntrospectQueries";

    /// <summary>
    /// Maps introspection endpoints for commands and queries.
    /// </summary>
    /// <param name="mapper">The <see cref="IEndpointMapper"/> to use.</param>
    public static void MapIntrospectionEndpoints(this IEndpointMapper mapper)
    {
        if (!mapper.EndpointExists(CommandsEndpointName))
        {
            mapper.MapGet(
                "/.cratis/commands",
                async context =>
                {
                    var introspectionService = context.RequestServices.GetRequiredService<IIntrospectionService>();
                    await context.WriteResponseAsJson(introspectionService.Commands, typeof(List<CommandIntrospectionMetadata>), context.RequestAborted);
                },
                new EndpointMetadata(
                    CommandsEndpointName,
                    "Introspect available command endpoints",
                    ["Cratis Introspection"],
                    AllowAnonymous: true,
                    ResponseType: typeof(List<CommandIntrospectionMetadata>)));
        }

        if (!mapper.EndpointExists(QueriesEndpointName))
        {
            mapper.MapGet(
                "/.cratis/queries",
                async context =>
                {
                    var introspectionService = context.RequestServices.GetRequiredService<IIntrospectionService>();
                    await context.WriteResponseAsJson(introspectionService.Queries, typeof(List<QueryIntrospectionMetadata>), context.RequestAborted);
                },
                new EndpointMetadata(
                    QueriesEndpointName,
                    "Introspect available query endpoints",
                    ["Cratis Introspection"],
                    AllowAnonymous: true,
                    ResponseType: typeof(List<QueryIntrospectionMetadata>)));
        }
    }
}
