// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.AspNetCore.EndpointGroups;

/// <summary>
/// Refuses to keep running when every listener port is assigned to a named endpoint group.
/// </summary>
/// <param name="services">The <see cref="IServiceProvider"/> to resolve the server from.</param>
/// <param name="options">The <see cref="ArcOptions"/> holding the port assignments.</param>
/// <remarks>
/// It runs after the server has started, when the addresses it listens on are known. Hosts that report no
/// addresses, such as the in-memory test server, are not checked.
/// </remarks>
public sealed class EndpointGroupListenerValidation(IServiceProvider services, IOptions<ArcOptions> options) : IHostedLifecycleService
{
    /// <inheritdoc/>
    public Task StartedAsync(CancellationToken cancellationToken)
    {
        var groups = options.Value.EndpointGroups;
        if (!groups.HasGroups)
        {
            return Task.CompletedTask;
        }

        var addresses = services.GetService<IServer>()?.Features.Get<IServerAddressesFeature>()?.Addresses ?? [];
        var ports = addresses.Select(PortOf).Where(_ => _ > 0).Distinct().ToArray();
        if (ports.Length > 0 && ports.All(groups.IsGroupPort))
        {
            throw new NoPublicListenerForDefaultEndpointGroup(ports);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task StartingAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <inheritdoc/>
    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <inheritdoc/>
    public Task StoppingAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <inheritdoc/>
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <inheritdoc/>
    public Task StoppedAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    static int PortOf(string address)
    {
        try
        {
            return BindingAddress.Parse(address).Port;
        }
        catch (FormatException)
        {
            return 0;
        }
    }
}
