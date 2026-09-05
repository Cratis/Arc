// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Serialization;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Arc.MongoDB.for_MongoDBWatcher.when_watching;

public class and_two_tenant_databases_observe
{
    [Fact]
    public async Task should_watch_each_tenant_database()
    {
        var namingPolicy = Substitute.For<INamingPolicy>();
        namingPolicy.GetReadModelName(Arg.Any<Type>()).Returns(ci => ci.Arg<Type>().Name.ToLowerInvariant());
        DatabaseExtensions.SetNamingPolicy(namingPolicy);

        var firstWatchStarted = new TaskCompletionSource();
        var secondWatchStarted = new TaskCompletionSource();

        var databaseA = BlockingDatabase(firstWatchStarted);
        var databaseB = BlockingDatabase(secondWatchStarted);

        var mongoClient = Substitute.For<IMongoClient>();
        mongoClient.GetDatabase("testdb+a").Returns(databaseA);
        mongoClient.GetDatabase("testdb+b").Returns(databaseB);

        var clientFactory = Substitute.For<IMongoDBClientFactory>();
        clientFactory.Create().Returns(mongoClient);

        // Successive tenants resolve to their own suffixed database names, mirroring DefaultMongoDatabaseNameResolver.
        var databaseNameResolver = Substitute.For<IMongoDatabaseNameResolver>();
        databaseNameResolver.Resolve().Returns("testdb+a", "testdb+b");

        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<MongoDBWatcher>>();

        using var watcher = new MongoDBWatcher(clientFactory, databaseNameResolver, logger);

        _ = watcher.Observe<BsonDocument>();
        _ = watcher.Observe<BsonDocument>();

        await Task.WhenAll(firstWatchStarted.Task, secondWatchStarted.Task).WaitAsync(TimeSpan.FromSeconds(5));

        mongoClient.Received(1).GetDatabase("testdb+a");
        mongoClient.Received(1).GetDatabase("testdb+b");
    }

    static IMongoDatabase BlockingDatabase(TaskCompletionSource watchStarted)
    {
        var database = Substitute.For<IMongoDatabase>();

        var cursor = Substitute.For<IChangeStreamCursor<ChangeStreamDocument<BsonDocument>>>();
        var blockTcs = new TaskCompletionSource<bool>();
        cursor.MoveNextAsync(Arg.Any<CancellationToken>()).Returns(_ =>
        {
            watchStarted.TrySetResult();
            return blockTcs.Task;
        });

        database.WatchAsync(
            Arg.Any<PipelineDefinition<ChangeStreamDocument<BsonDocument>, ChangeStreamDocument<BsonDocument>>>(),
            Arg.Any<ChangeStreamOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(cursor));

        var collection = Substitute.For<IMongoCollection<BsonDocument>>();
        collection.CollectionNamespace.Returns(new CollectionNamespace("testdb", "bsondocument"));
        collection.Database.Returns(database);
        database.GetCollection<BsonDocument>(Arg.Any<string>(), Arg.Any<MongoCollectionSettings>()).Returns(collection);

        return database;
    }
}
