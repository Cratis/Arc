// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reactive.Subjects;
using Cratis.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Arc.MongoDB;

/// <summary>
/// Represents an implementation of <see cref="IMongoDBWatcher"/> that maintains
/// a single change stream connection per database per process.
/// </summary>
/// <param name="clientFactory">The <see cref="IMongoDBClientFactory"/> for creating MongoDB clients.</param>
/// <param name="databaseNameResolver">The <see cref="IMongoDatabaseNameResolver"/> for resolving the database name.</param>
/// <param name="logger">The logger.</param>
/// <remarks>
/// The watch is partitioned by the resolved database name so a multi-tenant application — where the name resolver
/// appends the tenant to the database name — gets one change stream per tenant database rather than sharing the
/// first caller's. A single shared watcher that cached one database served every tenant the first tenant's data.
/// </remarks>
[Singleton]
public class MongoDBWatcher(
    IMongoDBClientFactory clientFactory,
    IMongoDatabaseNameResolver databaseNameResolver,
    ILogger<MongoDBWatcher> logger) : IMongoDBWatcher, IDisposable
{
    readonly ConcurrentDictionary<string, Lazy<DatabaseWatch>> _watches = new();
    readonly CancellationTokenSource _cts = new();

    /// <inheritdoc/>
    public IMongoDBObserveBuilder<TDocument> Observe<TDocument>(
        Expression<Func<TDocument, bool>>? filter = null)
    {
        var databaseName = databaseNameResolver.Resolve();
        var lazyWatch = _watches.GetOrAdd(
            databaseName,
            static (name, self) => new Lazy<DatabaseWatch>(() => self.StartWatch(name)),
            this);

        DatabaseWatch watch;
        try
        {
            watch = lazyWatch.Value;
        }
        catch
        {
            // A Lazy in the default ExecutionAndPublication mode caches a factory exception permanently. If starting
            // the watch failed — e.g. the client factory threw transiently — evict this exact entry so the next
            // Observe retries a fresh connection instead of replaying the cached failure for the process lifetime.
            // The keyed overload removes only while the stored value is still this poisoned Lazy, never a newer one.
            _watches.TryRemove(new KeyValuePair<string, Lazy<DatabaseWatch>>(databaseName, lazyWatch));
            throw;
        }

        var collection = watch.Database.GetCollection<TDocument>();
        return new MongoDBObserveBuilder<TDocument>(collection, filter, watch.Changes);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();

        foreach (var watch in _watches.Values.Where(_ => _.IsValueCreated).Select(_ => _.Value))
        {
            watch.Changes.OnCompleted();
            watch.Changes.Dispose();
        }

        _watches.Clear();
        GC.SuppressFinalize(this);
    }

    DatabaseWatch StartWatch(string databaseName)
    {
        var client = clientFactory.Create();
        var watch = new DatabaseWatch(client.GetDatabase(databaseName));
        _ = Task.Run(() => WatchDatabaseAsync(databaseName, watch));
        return watch;
    }

    async Task WatchDatabaseAsync(string databaseName, DatabaseWatch watch)
    {
        var delay = TimeSpan.FromSeconds(1);

        // Carried across reconnects so a transient failure resumes from the last delivered change instead of from
        // "now" — otherwise every change that occurred during the disconnect is silently and permanently missed.
        BsonDocument? resumeToken = null;

        while (!_cts.IsCancellationRequested)
        {
            try
            {
                var options = new ChangeStreamOptions
                {
                    FullDocument = ChangeStreamFullDocumentOption.UpdateLookup,
                    ResumeAfter = resumeToken
                };

                var pipeline = new EmptyPipelineDefinition<ChangeStreamDocument<BsonDocument>>();
                using var cursor = await watch.Database.WatchAsync(pipeline, options, _cts.Token);
                logger.StartedWatchingDatabase(databaseName);
                delay = TimeSpan.FromSeconds(1);

                await cursor.ForEachAsync(
                    changeDocument =>
                    {
                        resumeToken = changeDocument.ResumeToken;
                        try
                        {
                            watch.Changes.OnNext(changeDocument);
                        }
                        catch (Exception ex)
                        {
                            logger.UnexpectedError(ex);
                        }
                    },
                    _cts.Token);

                logger.DatabaseWatchCompleted(databaseName);
            }
            catch (OperationCanceledException)
            {
                logger.DatabaseWatchCancelled();
                return;
            }
            catch (Exception ex)
            {
                logger.DatabaseWatchReconnecting(databaseName, delay.TotalSeconds);
                logger.UnexpectedError(ex);
                try
                {
                    await Task.Delay(delay, _cts.Token);
                }
                catch (OperationCanceledException)
                {
                    return;
                }

                delay = TimeSpan.FromSeconds(Math.Min(delay.TotalSeconds * 2, 60));
            }
        }
    }

    sealed class DatabaseWatch(IMongoDatabase database)
    {
        public IMongoDatabase Database { get; } = database;

        public Subject<ChangeStreamDocument<BsonDocument>> Changes { get; } = new();
    }
}
