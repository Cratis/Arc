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
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Events;
using Polly;
using Polly.Retry;

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
    readonly ConcurrentDictionary<string, IMongoClient> _clients = new();
    readonly ConcurrentDictionary<string, int> _connectedClientsCount = new();
    readonly ConcurrentDictionary<string, int> _connectionsAddedToPoolCount = new();
    readonly ConcurrentDictionary<string, int> _checkedOutConnectionsCount = new();
    readonly ConcurrentDictionary<string, int> _commandsCount = new();

    /// <inheritdoc/>
    public IMongoClient Create() => Create(serverResolver.Resolve());

    /// <inheritdoc/>
#pragma warning disable MA0106 // Avoid closure by using an overload with the 'factoryArgument' parameter
    public IMongoClient Create(MongoClientSettings settings) => _clients.GetOrAdd(settings.Server.ToString(), (_) => CreateImplementation(settings));
#pragma warning restore MA0106 // Avoid closure by using an overload with the 'factoryArgument' parameter

    /// <inheritdoc/>
    public IMongoClient Create(MongoUrl url) => Create(MongoClientSettings.FromUrl(url));

    /// <inheritdoc/>
    public IMongoClient Create(string connectionString) => Create(MongoClientSettings.FromConnectionString(connectionString));

    IMongoClient CreateImplementation(MongoClientSettings settings)
    {
        settings.DirectConnection = options.Value.DirectConnection;
        settings.ClusterConfigurator = builder => ClusterConfigurator(settings, builder);

        logger.CreateClient(settings.Server.ToString());
#pragma warning disable CA2000 // Dispose objects before losing scope - we're returning the client
        var client = new MongoClient(settings);
#pragma warning restore CA2000 // Dispose objects before losing scope

        var resiliencePipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                ShouldHandle = args => args.Outcome.Exception is not null ? PredicateResult.True() : PredicateResult.False(),
                UseJitter = true,
                MaxRetryAttempts = 5,
                Delay = TimeSpan.FromMilliseconds(500)
            }).Build();

        var proxyGenerator = new ProxyGenerator();
        var proxyGeneratorOptions = new ProxyGenerationOptions
        {
            Selector = new MongoClientInterceptorSelector(proxyGenerator, resiliencePipeline, client)
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

        if (logger.IsEnabled(LogLevel.Trace))
        {
            builder
                .Subscribe<CommandStartedEvent>(CommandStarted)
                .Subscribe<CommandFailedEvent>(CommandFailed)
                .Subscribe<CommandSucceededEvent>(CommandSucceeded);
        }
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

    void CommandStarted(CommandStartedEvent command) => logger.CommandStarted(command.RequestId, command.CommandName, command.Command.ToJson());

    void CommandFailed(CommandFailedEvent command) => logger.CommandFailed(command.RequestId, command.CommandName, command.Failure.Message);

    void CommandSucceeded(CommandSucceededEvent command) => logger.CommandSucceeded(command.RequestId, command.CommandName);
}
