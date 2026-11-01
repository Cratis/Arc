// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Castle.DynamicProxy;
using Cratis.Arc.MongoDB.Resilience;
using Cratis.DependencyInjection;
using Cratis.Metrics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Events;
using Polly;

namespace Cratis.Arc.MongoDB;

/// <summary>
/// Represents an implementation of <see cref="IMongoDBClientFactory"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="MongoDBClientFactory"/> class.
/// </remarks>
/// <param name="serverResolver"><see cref="IMongoServerResolver"/> for resolving the server.</param>
/// <param name="meter"><see cref="IMeter{T}"/> for metering.</param>
/// <param name="options"><see cref="IOptions{TOptions}"/> for getting the options.</param>
/// <param name="logger"><see cref="ILogger"/> for logging.</param>
[Singleton]
public class MongoDBClientFactory(
    IMongoServerResolver serverResolver,
    [FromKeyedServices(Internals.MeterName)] IMeter<IMongoClient> meter,
    IOptions<MongoDBOptions> options,
    ILogger<MongoDBClientFactory> logger) : IMongoDBClientFactory
{
#pragma warning disable CA2000 // Dispose objects before losing scope - we're returning the client
    readonly Func<MongoClientSettings, IMongoClient> _createClient = settings => new MongoClient(settings);
#pragma warning restore CA2000 // Dispose objects before losing scope
    readonly ConcurrentDictionary<(string Servers, MongoCredential? Credential), IMongoClient> _clients = new();
    readonly ConcurrentDictionary<string, int> _connectedClientsCount = new();
    readonly ConcurrentDictionary<string, int> _connectionsAddedToPoolCount = new();
    readonly ConcurrentDictionary<string, int> _checkedOutConnectionsCount = new();
    readonly ConcurrentDictionary<string, int> _commandsCount = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="MongoDBClientFactory"/> class with a native client constructor.
    /// </summary>
    /// <param name="serverResolver">The server resolver.</param>
    /// <param name="meter">The client meter.</param>
    /// <param name="options">The MongoDB options.</param>
    /// <param name="logger">The factory logger.</param>
    /// <param name="createClient">The native client constructor.</param>
    internal MongoDBClientFactory(
        IMongoServerResolver serverResolver,
        IMeter<IMongoClient> meter,
        IOptions<MongoDBOptions> options,
        ILogger<MongoDBClientFactory> logger,
        Func<MongoClientSettings, IMongoClient> createClient) : this(serverResolver, meter, options, logger) => _createClient = createClient;

    /// <inheritdoc/>
    public IMongoClient Create() => Create(serverResolver.Resolve());

    /// <inheritdoc/>
    /// <remarks>
    /// Clients are shared per set of servers and credential, so settings that only differ in who they authenticate as
    /// never share a client.
    /// </remarks>
    public IMongoClient Create(MongoClientSettings settings) =>
        _clients.GetOrAdd(
            (string.Join(',', settings.Servers), settings.Credential),
            static (_, state) => state.Factory.CreateImplementation(state.Settings),
            (Factory: this, Settings: settings));

    /// <inheritdoc/>
    public IMongoClient Create(MongoUrl url) => Create(MongoClientSettings.FromUrl(url));

    /// <inheritdoc/>
    public IMongoClient Create(string connectionString) => Create(MongoClientSettings.FromConnectionString(connectionString));

    IMongoClient CreateImplementation(MongoClientSettings settings)
    {
        if (options.Value.DirectConnection is true)
        {
            settings.DirectConnection = true;
        }
        var configureCluster = settings.ClusterConfigurator;
        settings.ClusterConfigurator = builder =>
        {
            configureCluster?.Invoke(builder);
            ClusterConfigurator(settings, builder);
        };

        logger.CreateClient(settings.Server.ToString());
        var client = _createClient(settings);
        if (!options.Value.EnableResilience)
        {
            return new SharedMongoClient(client);
        }

        // No retry strategy: the collection interceptors complete the caller's task inside the callback for every
        // outcome, so nothing reaches a strategy and a retry could never trigger.
        var proxyGenerator = new ProxyGenerator();
        var proxyGeneratorOptions = new ProxyGenerationOptions
        {
            Selector = new MongoClientInterceptorSelector(proxyGenerator, ResiliencePipeline.Empty, client)
        };

        return proxyGenerator.CreateInterfaceProxyWithTarget<IMongoClient>(client, proxyGeneratorOptions);
    }

    void ClusterConfigurator(MongoClientSettings settings, ClusterBuilder builder)
    {
        var serverKey = settings.Server.ToString();
        var scope = meter.BeginScope(serverKey);

        UpdateConnectionCount(serverKey, scope, 0);
        UpdateConnectionsAddedToPool(serverKey, scope, 0);
        UpdateCheckedOutConnections(serverKey, scope, 0);
        UpdateCommandCount(serverKey, scope, 0);

        builder
            .Subscribe<ConnectionOpenedEvent>(_ => UpdateConnectionCount(serverKey, scope, _connectedClientsCount[serverKey] + 1))
            .Subscribe<ConnectionClosedEvent>(_ => UpdateConnectionCount(serverKey, scope, _connectedClientsCount[serverKey] - 1))
            .Subscribe<ConnectionOpeningFailedEvent>(_ =>
            {
                UpdateConnectionCount(serverKey, scope, _connectedClientsCount[serverKey] - 1);
                scope.FailedConnections();
            })
            .Subscribe<ConnectionPoolAddedConnectionEvent>(_ => UpdateConnectionsAddedToPool(serverKey, scope, _connectionsAddedToPoolCount[serverKey] + 1))
            .Subscribe<ConnectionPoolRemovedConnectionEvent>(_ => UpdateConnectionsAddedToPool(serverKey, scope, _connectionsAddedToPoolCount[serverKey] - 1))
            .Subscribe<ConnectionPoolCheckedOutConnectionEvent>(_ => UpdateCheckedOutConnections(serverKey, scope, _checkedOutConnectionsCount[serverKey] + 1))
            .Subscribe<ConnectionPoolCheckedInConnectionEvent>(_ => UpdateCheckedOutConnections(serverKey, scope, _checkedOutConnectionsCount[serverKey] - 1))
            .Subscribe<CommandStartedEvent>(_ =>
            {
                UpdateCommandCount(serverKey, scope, _commandsCount[serverKey] + 1);
                scope.AggregatedCommands();
            })
            .Subscribe<CommandSucceededEvent>(_ => UpdateCommandCount(serverKey, scope, _commandsCount[serverKey] - 1))
            .Subscribe<CommandFailedEvent>(_ => UpdateCommandCount(serverKey, scope, _commandsCount[serverKey] - 1));

        new MongoDBCommandLogging(logger, options.Value.LogCommandBodies).Subscribe(builder);
    }

    void UpdateConnectionCount(string serverKey, IMeterScope<IMongoClient> scope, int count)
    {
        _connectedClientsCount[serverKey] = count;
        scope.OpenConnections(count);
    }

    void UpdateConnectionsAddedToPool(string serverKey, IMeterScope<IMongoClient> scope, int count)
    {
        _connectionsAddedToPoolCount[serverKey] = count;
        scope.ConnectionsAddedToPool(count);
    }

    void UpdateCheckedOutConnections(string serverKey, IMeterScope<IMongoClient> scope, int count)
    {
        _checkedOutConnectionsCount[serverKey] = count;
        scope.CheckedOutConnections(count);
    }

    void UpdateCommandCount(string serverKey, IMeterScope<IMongoClient> scope, int count)
    {
        _commandsCount[serverKey] = count;
        scope.Commands(count);
    }
}
