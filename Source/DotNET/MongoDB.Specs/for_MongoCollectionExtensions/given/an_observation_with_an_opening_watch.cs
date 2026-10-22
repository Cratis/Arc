// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Arc.MongoDB.for_MongoCollectionExtensions.given;

public class an_observation_with_an_opening_watch : a_server_backed_observation
{
    protected TaskCompletionSource _watchRequested;
    protected TaskCompletionSource<IChangeStreamCursor<ChangeStreamDocument<BsonDocument>>> _openCursor;
    protected IChangeStreamCursor<ChangeStreamDocument<BsonDocument>> _cursor;
    MongoDBWatcher _sharedWatcher;

    void Establish()
    {
        _watchRequested = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _openCursor = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _cursor = Substitute.For<IChangeStreamCursor<ChangeStreamDocument<BsonDocument>>>();
        var hold = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        _cursor.MoveNextAsync(Arg.Any<CancellationToken>()).Returns(call => hold.Task.WaitAsync(call.Arg<CancellationToken>()));
        var database = _collection.Database;
        database.WatchAsync(
            Arg.Any<PipelineDefinition<ChangeStreamDocument<BsonDocument>, ChangeStreamDocument<BsonDocument>>>(),
            Arg.Any<ChangeStreamOptions>(),
            Arg.Any<CancellationToken>()).Returns(call =>
            {
                _watchRequested.TrySetResult();
                return _openCursor.Task.WaitAsync(call.Arg<CancellationToken>());
            });
        var client = Substitute.For<IMongoClient>();
        client.GetDatabase(DatabaseName).Returns(database);
        var factory = Substitute.For<IMongoDBClientFactory>();
        factory.Create().Returns(client);
        var resolver = Substitute.For<IMongoDatabaseNameResolver>();
        resolver.Resolve().Returns(DatabaseName);
        _sharedWatcher = new(factory, resolver, NullLogger<MongoDBWatcher>.Instance);
        var contextManager = Substitute.For<IQueryContextManager>();
        contextManager.Current.Returns(_ => _queryContext);
        Internals.ServiceProvider = new ServiceCollection()
            .AddLogging()
            .AddSingleton(contextManager)
            .AddSingleton<IMongoDBWatcher>(_sharedWatcher)
            .BuildServiceProvider();
    }

    void Destroy()
    {
        (_subject as IDisposable)?.Dispose();
        _sharedWatcher.Dispose();
    }
}
