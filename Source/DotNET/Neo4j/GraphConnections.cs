// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Arc.Connections;
using Cratis.Arc.Tenancy;
using Cratis.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Neo4j;

/// <summary>
/// Represents an implementation of <see cref="IGraphConnections"/>.
/// </summary>
/// <remarks>
/// Resolves through <see cref="Neo4jOptions.ConnectionResolution"/> when it is configured, else through the
/// application-wide <see cref="ArcOptions.ConnectionResolution"/>. Without either, the connection is the configured
/// server and credentials with the database <see cref="IGraphDatabaseNameResolver"/> names.
/// Holds no tenant state of its own: every resolution reads the current tenant, and everything kept is keyed by it.
/// </remarks>
/// <param name="options">The <see cref="IOptions{TOptions}"/> for <see cref="Neo4jOptions"/>.</param>
/// <param name="connectionResolvers">The <see cref="IConnectionResolvers"/> shared with the other storage integrations.</param>
/// <param name="databaseNameResolver">The <see cref="IGraphDatabaseNameResolver"/> giving the database name when none is resolved.</param>
/// <param name="tenantIdAccessor">The <see cref="ITenantIdAccessor"/>.</param>
/// <param name="driverFactory">The <see cref="INeo4jDriverFactory"/> creating the drivers.</param>
[Singleton]
public class GraphConnections(
    IOptions<Neo4jOptions> options,
    IConnectionResolvers connectionResolvers,
    IGraphDatabaseNameResolver databaseNameResolver,
    ITenantIdAccessor tenantIdAccessor,
    INeo4jDriverFactory driverFactory) : IGraphConnections
{
    readonly ConcurrentDictionary<string, Neo4jConnectionResolutionHooks> _hooks = new();

    /// <inheritdoc/>
    public GraphConnection Resolve()
    {
        var configuredServer = new Uri(options.Value.Server);
        var configuredAuthentication = driverFactory.GetConfiguredAuthentication();
        var resolver = connectionResolvers.For(options.Value.ConnectionResolution);
        if (resolver is null)
        {
            return new(driverFactory.Create(configuredServer, configuredAuthentication), databaseNameResolver.Resolve());
        }

        var request = ConnectionRequest.ForTenant(WellKnownConnectionStorage.Neo4j, ConnectionPurpose.Runtime, tenantIdAccessor.Current);

        // Resolved while creating scoped services, which cannot be awaited. A resolution is cached, so this only waits
        // when the cache is empty or expired.
        var resolved = resolver.Resolve(request).GetAwaiter().GetResult();
        var hooks = _hooks.GetOrAdd(
            request.ToKey(),
            static (_, state) => new(state.Resolver, state.Request, state.Configured),
            (Resolver: resolver, Request: request, Configured: configuredAuthentication));

        var server = string.IsNullOrEmpty(resolved.ConnectionString) ? configuredServer : new Uri(resolved.ConnectionString);
        return new(driverFactory.Create(server, hooks), resolved.Database ?? databaseNameResolver.Resolve());
    }
}
