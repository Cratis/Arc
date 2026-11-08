// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Capabilities;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Mcp;

/// <summary>Refuses startup when MCP is configured in a way that would not fail closed.</summary>
/// <param name="arc">The endpoint-group assignments.</param>
/// <param name="capabilities">The capability options.</param>
/// <param name="options">The MCP options.</param>
/// <param name="ceiling">The ceiling factory.</param>
/// <param name="services">The service provider, used to detect capability hosting.</param>
internal sealed class McpStartupValidation(
    IOptions<ArcOptions> arc,
    IOptions<CapabilitiesOptions> capabilities,
    IOptions<McpOptions> options,
    McpToolCeilingFactory ceiling,
    IServiceProvider services) : IHostedLifecycleService
{
    /// <inheritdoc/>
    /// <exception cref="McpRequiresCapabilities">Capability hosting is not enabled.</exception>
    /// <exception cref="CapabilityEndpointGroupHasNoPort">The capability group has no port.</exception>
    /// <exception cref="McpOptionsInvalid">Options are missing or invalid for the profile.</exception>
    /// <exception cref="InvalidMcpConfiguration">The exposure would not fail closed.</exception>
    public Task StartingAsync(CancellationToken cancellationToken)
    {
        if (services.GetService(typeof(CapabilityEndpointSelection)) is null)
        {
            throw new McpRequiresCapabilities();
        }

        var group = capabilities.Value.EndpointGroup;
        if (!arc.Value.EndpointGroups.PortsFor(group).Any())
        {
            throw new CapabilityEndpointGroupHasNoPort(group);
        }

        if (capabilities.Value.GatedEndpoints is null)
        {
            throw new McpRequiresGatedEndpoints();
        }

        ValidateOptions(options.Value);

        // Building the ceiling validates every declaration; a failure is cached and rethrown to every caller.
        _ = ceiling.Ceiling;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <inheritdoc/>
    public Task StartedAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <inheritdoc/>
    public Task StoppingAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <inheritdoc/>
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <inheritdoc/>
    public Task StoppedAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    static void ValidateOptions(McpOptions value)
    {
        if (string.IsNullOrWhiteSpace(value.Path) || !value.Path.StartsWith('/'))
        {
            throw new McpOptionsInvalid(nameof(McpOptions.Path), value.Profile);
        }

        if (value.MaxRequestBodySize <= 0)
        {
            throw new McpOptionsInvalid(nameof(McpOptions.MaxRequestBodySize), value.Profile);
        }

        if (value.AllowedOrigins.Any(_ => !McpOrigins.IsOrigin(_)))
        {
            throw new McpOptionsInvalid(nameof(McpOptions.AllowedOrigins), value.Profile);
        }

        if (value.Profile == McpProfile.PatOnly)
        {
            return;
        }

        if (!McpOrigins.IsOrigin(value.PublicOrigin))
        {
            throw new McpOptionsInvalid(nameof(McpOptions.PublicOrigin), value.Profile);
        }
        if (!McpOrigins.ResourceMatches(value))
        {
            throw new McpOptionsInvalid(nameof(McpOptions.Resource), value.Profile);
        }
        if (value.AuthorizationServers.Count == 0 || value.AuthorizationServers.Any(_ => !IsAbsoluteUri(_)))
        {
            throw new McpOptionsInvalid(nameof(McpOptions.AuthorizationServers), value.Profile);
        }
    }

    static bool IsAbsoluteUri(string? value) => Uri.TryCreate(value, UriKind.Absolute, out _);
}
