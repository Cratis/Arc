// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Introspection;

/// <summary>
/// Maps endpoint introspection routes.
/// </summary>
public static class IntrospectionEndpointMapper
{
    /// <summary>
    /// The command catalog endpoint name.
    /// </summary>
    internal const string CommandsEndpointName = "IntrospectCommands";

    /// <summary>
    /// The query catalog endpoint name.
    /// </summary>
    internal const string QueriesEndpointName = "IntrospectQueries";

    /// <summary>
    /// Maps introspection endpoints for commands and queries.
    /// </summary>
    /// <param name="mapper">The <see cref="IEndpointMapper"/> to use.</param>
    /// <remarks>This overload cannot resolve configured options from <see cref="IEndpointMapper"/> and always uses defaults.</remarks>
    [Obsolete("Use MapIntrospectionEndpoints(IEndpointMapper, IntrospectionOptions) to honor configured exposure options.")]
    public static void MapIntrospectionEndpoints(this IEndpointMapper mapper) => mapper.MapIntrospectionEndpoints(new IntrospectionOptions());

    /// <summary>
    /// Maps introspection endpoints using the configured exposure options.
    /// </summary>
    /// <param name="mapper">The <see cref="IEndpointMapper"/> to use.</param>
    /// <param name="options">The exposure options.</param>
    /// <exception cref="InvalidIntrospectionConfiguration">The catalog configuration is invalid or the host cannot enforce it.</exception>
    public static void MapIntrospectionEndpoints(this IEndpointMapper mapper, IntrospectionOptions options) =>
        mapper.MapIntrospectionEndpoints(options, logger: null);

    /// <summary>
    /// Maps introspection endpoints using the configured exposure options, reporting exposure that needs attention.
    /// </summary>
    /// <param name="mapper">The <see cref="IEndpointMapper"/> to use.</param>
    /// <param name="options">The exposure options.</param>
    /// <param name="logger">Optional logger to report exposure that needs attention.</param>
    /// <exception cref="InvalidIntrospectionConfiguration">The catalog configuration is invalid or the host cannot enforce it.</exception>
    internal static void MapIntrospectionEndpoints(this IEndpointMapper mapper, IntrospectionOptions options, ILogger? logger)
    {
        if (!options.Enabled)
        {
            DiscoveryExposure.ThrowIfInvalid(options);
            return;
        }

        var access = DiscoveryExposure.Resolve(mapper, options, logger);
        if (access == DiscoveryAccess.Unavailable)
        {
            return;
        }

        if (!mapper.EndpointExists(CommandsEndpointName))
        {
            mapper.MapGet(
                "/.cratis/commands",
                async context =>
                {
                    var introspectionService = context.RequestServices.GetRequiredService<IIntrospectionService>();
                    await context.WriteResponseAsJson(introspectionService.Commands, typeof(List<CommandIntrospectionMetadata>), context.RequestAborted);
                },
                DiscoveryExposure.MetadataFor(access, options, CommandsEndpointName, "Introspect available command endpoints", "Cratis Introspection", typeof(List<CommandIntrospectionMetadata>)));
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
                DiscoveryExposure.MetadataFor(access, options, QueriesEndpointName, "Introspect available query endpoints", "Cratis Introspection", typeof(List<QueryIntrospectionMetadata>)));
        }
    }
}
