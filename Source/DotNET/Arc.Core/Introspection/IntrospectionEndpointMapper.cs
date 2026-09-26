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
    public static void MapIntrospectionEndpoints(this IEndpointMapper mapper, IntrospectionOptions options)
    {
        var validation = IntrospectionOptionsValidator.ValidateOptions(options);
        if (validation.Failed)
        {
            throw new InvalidIntrospectionConfiguration(string.Join(' ', validation.Failures));
        }

        if (!options.Enabled)
        {
            return;
        }

        if (options.RequireAuthentication && mapper is IIntrospectionExposureGuard guard)
        {
            guard.Validate(options);
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
                new EndpointMetadata(
                    CommandsEndpointName,
                    "Introspect available command endpoints",
                    ["Cratis Introspection"],
                    AllowAnonymous: !options.RequireAuthentication,
                    ResponseType: typeof(List<CommandIntrospectionMetadata>))
                {
                    RequireAuthentication = options.RequireAuthentication,
                    Roles = options.Roles
                });
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
                    AllowAnonymous: !options.RequireAuthentication,
                    ResponseType: typeof(List<QueryIntrospectionMetadata>))
                {
                    RequireAuthentication = options.RequireAuthentication,
                    Roles = options.Roles
                });
        }
    }
}
