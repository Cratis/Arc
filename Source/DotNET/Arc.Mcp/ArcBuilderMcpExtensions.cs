// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

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
    public static IArcBuilder WithMcp(this IArcBuilder builder, Action<McpBuilder>? configure = null)
    {
        var services = builder.Services;
        services.AddOptions<McpOptions>();
        services.TryAddSingleton<McpToolCeilingFactory>();
        services.TryAddSingleton(provider => provider.GetRequiredService<McpToolCeilingFactory>().Ceiling);
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, McpStartupValidation>());

        var exposure = (McpExposure?)services.FirstOrDefault(_ => _.ServiceType == typeof(McpExposure))?.ImplementationInstance;
        if (exposure is null)
        {
            exposure = new McpExposure();
            services.AddSingleton(exposure);
        }

        configure?.Invoke(new McpBuilder(services, exposure));
        return builder;
    }
}
