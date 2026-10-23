// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Linq.Expressions;
using Cratis.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Arc.MongoDB;

/// <summary>
/// Represents an implementation of <see cref="IMongoDBWatcher"/> that maintains
/// a single change stream connection per database per deployment per process.
/// </summary>
/// <param name="clientFactory">
/// The <see cref="IMongoDBClientFactory"/> for the application's configured client, or null when MongoDB is not
/// configured. Only watches requested by database name need it; a watch for a given <see cref="IMongoDatabase"/> uses
/// that database's own client.
/// </param>
/// <param name="databaseNameResolver">
/// The <see cref="IMongoDatabaseNameResolver"/> for the application's database name, or null when MongoDB is not
/// configured. Only <see cref="Observe{TDocument}"/> needs it.
/// </param>
/// <param name="logger">The logger.</param>
/// <param name="connections">Optional <see cref="IMongoDBConnections"/>, so watches follow configured connection resolution.</param>
/// <remarks>
/// The watch is partitioned by the resolved database name so a multi-tenant application — where the name resolver
/// appends the tenant to the database name — gets one change stream per tenant database rather than sharing the
/// first caller's. A single shared watcher that cached one database served every tenant the first tenant's data.
/// With connection resolution the watch is also partitioned by server, since two tenants may resolve to databases of the
/// same name on different servers.
/// A watch is keyed by the deployment its client connects to as well as the database name, so a database that came
/// from another client - another library's storage, for instance - is watched where it lives, not on the application's
/// configured server.
/// </remarks>
[Singleton]
public class MongoDBWatcher(
    IMongoDBClientFactory? clientFactory,
    IMongoDatabaseNameResolver? databaseNameResolver,
    ILogger<MongoDBWatcher> logger,
    IMongoDBConnections? connections = null) : IMongoDBWatcher, IDisposable
{
    readonly ConcurrentDictionary<WatchKey, Lazy<MongoDBDatabaseWatch>> _watches = new();
    readonly CancellationTokenSource _cts = new();
    readonly object _lifetimeGate = new();
    readonly List<Task> _workers = [];
    bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="MongoDBWatcher"/> class for a host that has not configured MongoDB,
    /// and only observes collections and databases it brings itself.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public MongoDBWatcher(ILogger<MongoDBWatcher> logger)
        : this(null, null, logger)
    {
    }

    /// <inheritdoc/>
    public IMongoDBObserveBuilder<TDocument> Observe<TDocument>(
        Expression<Func<TDocument, bool>>? filter = null)
    {
        lock (_lifetimeGate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return CreateObservation<TDocument>(filter);
        }
    }

    /// <inheritdoc/>
    public IObservable<ChangeStreamDocument<BsonDocument>> GetChanges(string databaseName)
    {
        lock (_lifetimeGate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return GetWatch(databaseName, connections?.Resolve()?.Client ?? DefaultClient());
        }
    }

    /// <inheritdoc/>
    public IObservable<ChangeStreamDocument<BsonDocument>> GetChanges(IMongoDatabase database)
    {
        lock (_lifetimeGate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return GetWatch(database.DatabaseNamespace.DatabaseName, database.Client);
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        lock (_lifetimeGate)
        {
            if (_disposed)
            {
                return;
            }
            _disposed = true;
        }

        _cts.Cancel();
        _ = DisposeWhenStopped();
        GC.SuppressFinalize(this);
    }

    MongoDBObserveBuilder<TDocument> CreateObservation<TDocument>(Expression<Func<TDocument, bool>>? filter)
    {
        var connection = connections?.Resolve();
        var databaseName = connection?.DatabaseName ?? (databaseNameResolver ?? throw new MongoDBWatcherNotConfigured(nameof(IMongoDatabaseNameResolver))).Resolve();
        var watch = GetWatch(databaseName, connection?.Client ?? DefaultClient());
        var collection = watch.Database.GetCollection<TDocument>();
        return new MongoDBObserveBuilder<TDocument>(collection, filter, watch, logger);
    }

    MongoDBDatabaseWatch GetWatch(string databaseName, IMongoClient client)
    {
        var key = WatchKey.For(client, databaseName);
        var lazyWatch = _watches.GetOrAdd(
            key,
            static (_, state) => new Lazy<MongoDBDatabaseWatch>(() => state.Self.StartWatch(state.Name, state.Client)),
            (Self: this, Name: databaseName, Client: client));

        try
        {
            return lazyWatch.Value;
        }
        catch
        {
            // A Lazy in the default ExecutionAndPublication mode caches a factory exception permanently. If starting
            // the watch failed — e.g. the client threw transiently — evict this exact entry so the next call retries a
            // fresh connection instead of replaying the cached failure for the process lifetime.
            // The keyed overload removes only while the stored value is still this poisoned Lazy, never a newer one.
            _watches.TryRemove(new KeyValuePair<WatchKey, Lazy<MongoDBDatabaseWatch>>(key, lazyWatch));
            throw;
        }
    }

    IMongoClient DefaultClient() => (clientFactory ?? throw new MongoDBWatcherNotConfigured(nameof(IMongoDBClientFactory))).Create();

    MongoDBDatabaseWatch StartWatch(string databaseName, IMongoClient client)
    {
        var watch = new MongoDBDatabaseWatch(client.GetDatabase(databaseName), logger, _cts.Token);
        _workers.Add(Task.Run(() => watch.Run(databaseName)));
        return watch;
    }

    async Task DisposeWhenStopped()
    {
        try
        {
            await Task.WhenAll(_workers);
        }
        finally
        {
            _watches.Clear();
            _cts.Dispose();
        }
    }
}
