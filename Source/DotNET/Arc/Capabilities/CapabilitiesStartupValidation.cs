// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Capabilities;

/// <summary>Refuses startup when the capability group has no port or no usable credential installation.</summary>
/// <param name="arc">The endpoint-group assignments.</param>
/// <param name="capabilities">The capability options.</param>
/// <param name="installation">The verified-caller installation.</param>
/// <param name="catalog">The resource-to-audience catalog.</param>
/// <param name="scopes">The factory for resolving scoped credential sources.</param>
internal sealed class CapabilitiesStartupValidation(
    IOptions<ArcOptions> arc,
    IOptions<CapabilitiesOptions> capabilities,
    VerifiedCallerInstallation installation,
    CallerResourceCatalog catalog,
    IServiceScopeFactory scopes) : IHostedLifecycleService
{
    /// <inheritdoc/>
    /// <exception cref="CapabilityEndpointGroupHasNoPort">The capability group has no port.</exception>
    /// <exception cref="CapabilityHostingHasNoCredentialInstallation">The verified-caller installation is not usable.</exception>
    public Task StartingAsync(CancellationToken cancellationToken)
    {
        var group = capabilities.Value.EndpointGroup;
        if (!arc.Value.EndpointGroups.PortsFor(group).Any())
        {
            throw new CapabilityEndpointGroupHasNoPort(group);
        }
        new CapabilityEndpointSelection(capabilities).Validate();
        if (string.IsNullOrWhiteSpace(installation.Resource) || string.IsNullOrWhiteSpace(installation.Environment))
        {
            throw new CapabilityHostingHasNoCredentialInstallation("the installation names no resource and environment");
        }
        if (!installation.IsConfigured(catalog))
        {
            throw new CapabilityHostingHasNoCredentialInstallation("the resource has no single receiving audience in the resource catalog");
        }

        using var scope = scopes.CreateScope();
        var sources = scope.ServiceProvider.GetServices<IVerifiedCredentialSource>().ToArray();
        if (sources.Length == 0)
        {
            throw new CapabilityHostingHasNoCredentialInstallation("no credential verifier is registered");
        }
        if (scope.ServiceProvider.GetService<PatCapableCallerInstallation>() is not null && !sources.Any(source => source.Shape == PresentedCredentialShape.PatBearer))
        {
            throw new CapabilityHostingHasNoCredentialInstallation("no PAT credential verifier is registered for the PAT-capable installation");
        }

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
}
