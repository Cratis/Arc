// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Arc.Connections;
using Cratis.Arc.Tenancy;
using Cratis.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.MongoDB;

/// <summary>
/// Represents an implementation of <see cref="IMongoDBConnections"/>.
/// </summary>
/// <remarks>
/// Without configured connection resolution, the connection is the one <see cref="IMongoServerResolver"/> and
/// <see cref="IMongoDatabaseNameResolver"/> describe — exactly as without this type.
/// Holds no tenant state of its own: every resolution reads the current tenant, and everything kept is keyed by it.
/// </remarks>
/// <param name="options">The <see cref="IOptions{TOptions}"/> for <see cref="MongoDBOptions"/>.</param>
/// <param name="resolverFactory">The <see cref="IConnectionResolverFactory"/>.</param>
/// <param name="serverResolver">The <see cref="IMongoServerResolver"/> giving the configured server.</param>
/// <param name="databaseNameResolver">The <see cref="IMongoDatabaseNameResolver"/> giving the database name when none is resolved.</param>
/// <param name="tenantIdAccessor">The <see cref="ITenantIdAccessor"/>.</param>
/// <param name="clientFactory">The <see cref="IMongoDBClientFactory"/> creating the clients.</param>
[Singleton]
public class MongoDBConnections(
    IOptions<MongoDBOptions> options,
    IConnectionResolverFactory resolverFactory,
    IMongoServerResolver serverResolver,
    IMongoDatabaseNameResolver databaseNameResolver,
    ITenantIdAccessor tenantIdAccessor,
    IMongoDBClientFactory clientFactory) : IMongoDBConnections
{
    readonly ConcurrentDictionary<string, MongoDBConnectionResolutionHooks> _hooks = new();
    readonly Lazy<ICachingConnectionResolver> _resolver = new(() => resolverFactory.Create(options.Value.ConnectionResolution));

    /// <inheritdoc/>
    public MongoDBConnection Resolve()
    {
        if (!options.Value.ConnectionResolution.IsConfigured)
        {
            return new(clientFactory.Create(), databaseNameResolver.Resolve());
        }

        var resolver = _resolver.Value;
        var request = ConnectionRequest.ForTenant(WellKnownConnectionStorage.MongoDB, ConnectionPurpose.Runtime, tenantIdAccessor.Current);

        // Resolved while creating scoped services, which cannot be awaited. A resolution is cached, so this only waits
        // when the cache is empty or expired.
        var resolved = resolver.Resolve(request).GetAwaiter().GetResult();
        var hooks = _hooks.GetOrAdd(request.ToKey(), static (_, state) => new(state.Resolver, state.Request, TimeProvider.System), (Resolver: resolver, Request: request));

        var settings = resolved.ToMongoClientSettings(serverResolver.Resolve(), hooks);
        settings.ClusterConfigurator = hooks.ConfigureCluster;

        return new(clientFactory.Create(settings), resolved.Database ?? databaseNameResolver.Resolve());
    }
}
