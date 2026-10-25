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
internal sealed class ChronicleStartupDecisionReads(IServiceProvider services) : IStartupDecisionReads
{
    /// <inheritdoc/>
    public async Task<IDecisionReads?> Get(CancellationToken cancellationToken)
    {
        var options = services.GetService<IOptions<ChronicleClientOptions>>()?.Value;
        if (options?.AutoDiscoverAndRegister != true)
        {
            return null;
        }

        var client = services.GetService<IChronicleClient>();
        if (client is null)
        {
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
