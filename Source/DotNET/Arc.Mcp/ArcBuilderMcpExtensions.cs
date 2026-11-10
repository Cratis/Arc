// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Capabilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using ModelContextProtocol.AspNetCore;
using ModelContextProtocol.Server;

namespace Cratis.Arc.Mcp;

/// <summary>
/// Provides extensions for serving MCP from Arc.
/// </summary>
public static class ArcBuilderMcpExtensions
{
    /// <summary>
    /// Enables MCP serving. Nothing is exposed unless declared, and capability hosting is required.
    /// </summary>
    /// <param name="builder">The Arc builder.</param>
    /// <param name="configure">An optional delegate declaring options and exposed operations.</param>
    /// <returns>The builder for continuation.</returns>
    /// <remarks>
    /// Map the endpoint with <see cref="McpEndpointRouteBuilderExtensions.MapArcMcp"/>. The endpoint is served
    /// only on the capabilities endpoint group, behind capability gating (the whole port, or the selection in
    /// <see cref="CapabilitiesOptions.GatedEndpoints"/>), and is stateless: every request
    /// is one call that runs in the scope of its HTTP request, as the caller the capability gate verified.
    /// </remarks>
    public static IArcBuilder WithMcp(this IArcBuilder builder, Action<McpBuilder>? configure = null)
    {
        var services = builder.Services;
        services.AddOptions<McpOptions>();
        services.AddHttpContextAccessor();

        // Replace, not TryAdd: Arc's convention and self bindings may already have registered these with another
        // lifetime, and the catalog must be built exactly once so every part of MCP sees the same frozen tools.
        services.Replace(ServiceDescriptor.Singleton<McpToolCatalogFactory, McpToolCatalogFactory>());
        services.Replace(ServiceDescriptor.Singleton(provider => provider.GetRequiredService<McpToolCatalogFactory>().Catalog));
        services.Replace(ServiceDescriptor.Singleton<McpCallAudit, McpCallAudit>());
        services.Replace(ServiceDescriptor.Singleton<McpAdmission, McpAdmission>());
        services.Replace(ServiceDescriptor.Singleton<McpRefusalWriter, McpRefusalWriter>());
        services.Replace(ServiceDescriptor.Singleton<McpToolDispatcher, McpToolDispatcher>());
        services.Replace(ServiceDescriptor.Singleton<McpOperations, McpOperations>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, McpStartupValidation>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IPostConfigureOptions<McpServerOptions>, McpServerToolsConfiguration>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IPostConfigureOptions<CapabilitiesOptions>, McpGatedEndpoints>());

        var exposure = services.Where(_ => !_.IsKeyedService).Select(_ => _.ImplementationInstance).OfType<McpExposure>().FirstOrDefault();
        if (exposure is null)
        {
            exposure = new McpExposure();
            services.Replace(ServiceDescriptor.Singleton(exposure));
            services
                .AddMcpServer()
                .WithHttpTransport(_ => _.SessionMode = HttpServerSessionMode.Stateless)
                .WithRequestFilters(_ => _.AddListToolsFilter(McpToolListFilter.Filter));
        }

        configure?.Invoke(new McpBuilder(services, exposure));
        return builder;
    }
}
