// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.AspNetCore.EndpointGroups;
using Cratis.Arc.Capabilities;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Mcp;

/// <summary>
/// Extension methods for mapping the Arc MCP endpoint.
/// </summary>
public static class McpEndpointRouteBuilderExtensions
{
    /// <summary>
    /// Maps the MCP endpoint at <see cref="McpOptions.Path"/> on the capabilities endpoint group, together with its
    /// protected resource metadata.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <returns>The builder for the MCP endpoint, for further conventions.</returns>
    /// <remarks>
    /// Requires <see cref="ArcBuilderMcpExtensions.WithMcp"/>. Every request to the MCP endpoint passes the capability
    /// gate, which refuses in the MCP shape, and is admitted only for a verified caller: the body is size-bounded,
    /// parsed strictly, and a call to a tool the caller's scopes do not cover is refused before it reaches the server.
    /// </remarks>
    public static IEndpointConventionBuilder MapArcMcp(this IEndpointRouteBuilder endpoints)
    {
        var services = endpoints.ServiceProvider;
        var options = services.GetRequiredService<IOptions<McpOptions>>().Value;
        var group = new EndpointGroupMetadata(services.GetRequiredService<IOptions<CapabilitiesOptions>>().Value.EndpointGroup);
        var refusals = services.GetRequiredService<McpRefusalWriter>();

        var mcp = endpoints.MapMcp(options.Path);
        Admit(mcp, group, refusals);

        var otherMethods = endpoints.MapMethods(options.Path, [HttpMethods.Get, HttpMethods.Delete], _ => Task.CompletedTask);
        Admit(otherMethods, group, refusals);

        endpoints.MapGet(McpChallenge.MetadataPath(options), McpProtectedResourceMetadata.Serve)
            .WithMetadata(group);

        return mcp;
    }

    /// <summary>
    /// Maps the MCP endpoint. Obsolete name of <see cref="MapArcMcp"/>.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <returns>The builder for the MCP endpoint, for further conventions.</returns>
    [Obsolete("Use MapArcMcp. Will be removed in Arc 24.")]
    public static IEndpointConventionBuilder MapCratisArcMcp(this IEndpointRouteBuilder endpoints) => endpoints.MapArcMcp();

    static void Admit(IEndpointConventionBuilder builder, EndpointGroupMetadata group, McpRefusalWriter refusals)
    {
        builder.WithMetadata(group, McpEndpointMetadata.Instance, refusals, AcceptsOAuthCapability.Instance);
        builder.Add(endpoint =>
        {
            if (endpoint.RequestDelegate is not { } inner)
            {
                return;
            }

            endpoint.RequestDelegate = context => context.RequestServices.GetRequiredService<McpAdmission>().Invoke(context, inner);
        });
    }
}
