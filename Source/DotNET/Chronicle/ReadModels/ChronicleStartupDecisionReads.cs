// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.ReadModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Chronicle.ReadModels;

/// <summary>
/// Obtains admission metadata without resolving a tenant-dependent command reader.
/// </summary>
/// <param name="services">The host services.</param>
/// <param name="clientOptions">Resolves the client options the host configured Chronicle with.</param>
internal sealed class ChronicleStartupDecisionReads(IServiceProvider services, Func<IServiceProvider, ChronicleClientOptions?> clientOptions) : IStartupDecisionReads
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ChronicleStartupDecisionReads"/> class for hosts that configure <see cref="ChronicleClientOptions"/>.
    /// </summary>
    /// <param name="services">The host services.</param>
    public ChronicleStartupDecisionReads(IServiceProvider services)
        : this(services, provider => provider.GetService<IOptions<ChronicleClientOptions>>()?.Value)
    {
    }

    /// <inheritdoc/>
    public async Task<IDecisionReads?> Get(CancellationToken cancellationToken)
    {
        var options = clientOptions(services);
        var client = services.GetService<IChronicleClient>();
        if (options?.AutoDiscoverAndRegister != true || client is null)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(options.EventStore) && client.Options is ChronicleClientOptions effective)
        {
            // Hosts that configure Chronicle through derived options (such as ASP.NET Core) hand them to the client.
            options = effective;
        }

        if (string.IsNullOrWhiteSpace(options.EventStore))
        {
            // Without a resolved event store name, asking the client would register an unnamed event store.
            return null;
        }

        try
        {
            var store = await client.GetEventStore(options.EventStore).WaitAsync(cancellationToken);
            return store.GetDecisionReads();
        }
        catch (Exception exception) when (exception is ConnectionUnavailable or ConnectionTimedOut)
        {
            // UseCratisChronicle reports connection failures; admission must not make them fatal.
            return null;
        }
    }
}
