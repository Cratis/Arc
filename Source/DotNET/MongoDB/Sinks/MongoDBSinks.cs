// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Cratis.Arc.MongoDB.Sinks;

/// <summary>
/// Represents an implementation of <see cref="IMongoDBSinks"/>.
/// </summary>
/// <remarks>
/// Clients come from <see cref="IMongoDBClientFactory"/>, so a sink gets the same resilience, metrics and
/// <see cref="MongoDBOptions.DirectConnection"/> as the default connection, and sinks on the same server share a client.
/// </remarks>
/// <param name="options">The <see cref="IOptions{TOptions}"/> for <see cref="MongoDBOptions"/>.</param>
/// <param name="clientFactory">The <see cref="IMongoDBClientFactory"/> creating the clients.</param>
[Singleton]
public class MongoDBSinks(IOptions<MongoDBOptions> options, IMongoDBClientFactory clientFactory) : IMongoDBSinks
{
    readonly ConcurrentDictionary<string, IMongoClient> _clients = new(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc/>
    public IMongoClient GetClientFor(string sinkName) =>
        _clients.GetOrAdd(sinkName, static (name, self) => self.CreateClient(name), this);

    IMongoClient CreateClient(string sinkName)
    {
        var sinks = options.Value.Sinks;
        var sink = sinks.FirstOrDefault(_ => string.Equals(_.Key, sinkName, StringComparison.OrdinalIgnoreCase));
        if (sink.Value is null)
        {
            throw new UnknownMongoDBSink(sinkName, sinks.Keys);
        }

        return clientFactory.Create(new MongoUrl(MongoDBSinkServers.GetServer(sink.Key, sink.Value)));
    }
}
