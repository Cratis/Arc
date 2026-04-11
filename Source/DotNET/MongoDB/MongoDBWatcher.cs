// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
[Singleton]
public class MongoDBWatcher(
    IMongoDBClientFactory clientFactory,
    IMongoDatabaseNameResolver databaseNameResolver,
    ILogger<MongoDBWatcher> logger) : IMongoDBWatcher, IDisposable
{
    readonly object _databaseLock = new();
    readonly Subject<ChangeStreamDocument<BsonDocument>> _databaseChanges = new();
    readonly object _watchLock = new();
    readonly CancellationTokenSource _cts = new();
    volatile IMongoDatabase? _database;
    volatile bool _watching;

    /// <inheritdoc/>
    public IMongoDBObserveBuilder<TDocument> Observe<TDocument>(
        Expression<Func<TDocument, bool>>? filter = null)
    {
        EnsureWatching();
        var collection = GetDatabase().GetCollection<TDocument>();
        return new MongoDBObserveBuilder<TDocument>(collection, filter, _databaseChanges);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
        _databaseChanges.OnCompleted();
        _databaseChanges.Dispose();
        GC.SuppressFinalize(this);
    }

    IMongoDatabase GetDatabase()
    {
        if (_database is not null)
        {
            return _database;
        }

        lock (_databaseLock)
        {
            if (_database is null)
            {
                var client = clientFactory.Create();
                _database = client.GetDatabase(databaseNameResolver.Resolve());
            }
        }

        return _database;
    }

    void EnsureWatching()
    {
        if (_watching)
        {
            return;
        }

        lock (_watchLock)
        {
            if (_watching)
            {
                return;
            }

            _watching = true;
            _ = Task.Run(WatchDatabaseAsync);
        }
    }

    async Task WatchDatabaseAsync()
    {
        try
        {
            var databaseName = databaseNameResolver.Resolve();
            var database = GetDatabase();
            var options = new ChangeStreamOptions
            {
                FullDocument = ChangeStreamFullDocumentOption.UpdateLookup
            };

            var pipeline = new EmptyPipelineDefinition<ChangeStreamDocument<BsonDocument>>();
            using var cursor = await database.WatchAsync(pipeline, options, _cts.Token);
            logger.StartedWatchingDatabase(databaseName);

            await cursor.ForEachAsync(
                changeDocument =>
                {
                    try
                    {
                        _databaseChanges.OnNext(changeDocument);
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
        }
        catch (Exception ex)
        {
            logger.UnexpectedError(ex);
        }
    }
}
