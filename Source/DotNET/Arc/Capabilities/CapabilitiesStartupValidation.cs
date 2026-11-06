// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Capabilities;

/// <summary>Refuses startup when the capability group has no assigned port.</summary>
/// <param name="arc">The endpoint-group assignments.</param>
/// <param name="capabilities">The capability options.</param>
internal sealed class CapabilitiesStartupValidation(IOptions<ArcOptions> arc, IOptions<CapabilitiesOptions> capabilities) : IHostedLifecycleService
{
    /// <inheritdoc/>
    /// <exception cref="CapabilityEndpointGroupHasNoPort">The capability group has no port.</exception>
    public Task StartingAsync(CancellationToken cancellationToken)
    {
        var group = capabilities.Value.EndpointGroup;
        if (!arc.Value.EndpointGroups.PortsFor(group).Any())
        {
            throw new CapabilityEndpointGroupHasNoPort(group);
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
