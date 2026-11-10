// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Capabilities;
using Cratis.Arc.Identity.Callers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;

namespace Cratis.Arc.Mcp;

/// <summary>Refuses startup when MCP is configured in a way that would not fail closed.</summary>
/// <param name="arc">The endpoint-group assignments.</param>
/// <param name="capabilities">The capability options.</param>
/// <param name="options">The MCP options.</param>
/// <param name="catalog">The catalog factory.</param>
/// <param name="services">The service provider, used to detect capability hosting.</param>
/// <param name="allowlists">The configured credential scope allow lists.</param>
/// <param name="logger">The startup diagnostics logger.</param>
internal sealed class McpStartupValidation(
    IOptions<ArcOptions> arc,
    IOptions<CapabilitiesOptions> capabilities,
    IOptions<McpOptions> options,
    McpToolCatalogFactory catalog,
    IServiceProvider services,
    IEnumerable<ICredentialScopeAllowlist> allowlists,
    ILogger<McpStartupValidation> logger) : IHostedLifecycleService
{
    /// <inheritdoc/>
    /// <exception cref="McpRequiresCapabilities">Capability hosting is not enabled.</exception>
    /// <exception cref="CapabilityEndpointGroupHasNoPort">The capability group has no port.</exception>
    /// <exception cref="McpOptionsInvalid">Options are missing or invalid for the profile.</exception>
    /// <exception cref="InvalidMcpConfiguration">The exposure would not fail closed.</exception>
    /// <exception cref="McpToolNameTakenByServerTool">A tool registered on the MCP server has the name of a catalog tool.</exception>
    public Task StartingAsync(CancellationToken cancellationToken)
    {
        if (!services.HasCapabilities())
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

        // Building the catalog validates every declaration; a failure is cached and rethrown to every caller.
        WarnAboutPatScopes(catalog.Catalog);

        // Resolving the server options merges the catalog with the server's own tools, so a name collision fails here.
        _ = (services.GetService(typeof(IOptions<McpServerOptions>)) as IOptions<McpServerOptions>)?.Value;
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

        if (value.PublicOrigin is not null && !McpOrigins.IsOrigin(value.PublicOrigin))
        {
            throw new McpOptionsInvalid(nameof(McpOptions.PublicOrigin), value.Profile);
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
        if (value.AuthorizationServers.Count == 0 || value.AuthorizationServers.Any(_ => !IsAuthorizationServer(_)))
        {
            throw new McpOptionsInvalid(nameof(McpOptions.AuthorizationServers), value.Profile);
        }
    }

    static bool IsAuthorizationServer(string? value) => Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
        string.IsNullOrEmpty(uri.UserInfo) && string.IsNullOrEmpty(uri.Query) && string.IsNullOrEmpty(uri.Fragment) &&
        (uri.Scheme == Uri.UriSchemeHttps || (uri.Scheme == Uri.UriSchemeHttp && uri.IsLoopback));

    void WarnAboutPatScopes(McpToolCatalog tools)
    {
        foreach (var allowlist in allowlists.Where(_ => _.Kind == CallerCredentialKind.PersonalAccessToken))
        {
            // A tool a PAT can satisfy needs no warning; only scopes of tools it can never satisfy are reported.
            var missing = tools.Tools
                .Where(tool => !tool.IsSatisfiedBy(allowlist.Scopes))
                .SelectMany(tool => tool.RequiredScopes.Concat((tool.AlternativeScopes ?? []).SelectMany(_ => _)))
                .Where(scope => !string.IsNullOrWhiteSpace(scope) && !allowlist.Scopes.Contains(scope))
                .Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal);
            foreach (var scope in missing)
            {
                logger.PatScopeMissing(scope);
            }
        }
    }
}
