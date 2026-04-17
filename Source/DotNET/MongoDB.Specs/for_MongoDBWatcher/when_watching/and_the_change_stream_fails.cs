// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Serialization;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Arc.MongoDB.for_MongoDBWatcher.when_watching;

public class and_the_change_stream_fails
{
    [Fact]
    public async Task should_retry_watching_after_transient_failure()
    {
        // Configure the global naming policy required by DatabaseExtensions.GetCollection<T>()
        var namingPolicy = Substitute.For<INamingPolicy>();
        namingPolicy.GetReadModelName(Arg.Any<Type>()).Returns(ci => ci.Arg<Type>().Name.ToLowerInvariant());
        DatabaseExtensions.SetNamingPolicy(namingPolicy);

        var callCount = 0;
        var secondCallStarted = new TaskCompletionSource();

        var database = Substitute.For<IMongoDatabase>();

        // First call: throw a transient exception
        // Second call: succeed but block forever (until cancelled)
        var blockingCursor = Substitute.For<IChangeStreamCursor<ChangeStreamDocument<BsonDocument>>>();
#pragma warning disable CA2025 // Intentional: test uses a never-completing task to keep the watch loop alive
        var blockTcs = new TaskCompletionSource<bool>();
        blockingCursor.MoveNextAsync(Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                secondCallStarted.TrySetResult();
                return blockTcs.Task;
            });
#pragma warning restore CA2025

        database.WatchAsync(
            Arg.Any<PipelineDefinition<ChangeStreamDocument<BsonDocument>, ChangeStreamDocument<BsonDocument>>>(),
            Arg.Any<ChangeStreamOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                callCount++;
                if (callCount == 1)
                    throw new Exception("Simulated transient failure");

#pragma warning disable CA2025 // Intentional: test uses a cursor wrapping a disposable for a never-completing task
                return Task.FromResult(blockingCursor);
#pragma warning restore CA2025
            });

        var mongoClient = Substitute.For<IMongoClient>();
        mongoClient.GetDatabase(Arg.Any<string>()).Returns(database);

        // GetCollection<T> must succeed so Observe<BsonDocument>() can build the builder
        var collection = Substitute.For<IMongoCollection<BsonDocument>>();
        collection.CollectionNamespace.Returns(new CollectionNamespace("testdb", "bsondocument"));
        collection.Database.Returns(database);
        database.GetCollection<BsonDocument>(Arg.Any<string>(), Arg.Any<MongoCollectionSettings>())
            .Returns(collection);

        var clientFactory = Substitute.For<IMongoDBClientFactory>();
        clientFactory.Create().Returns(mongoClient);

        var databaseNameResolver = Substitute.For<IMongoDatabaseNameResolver>();
        databaseNameResolver.Resolve().Returns("testdb");

        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<MongoDBWatcher>>();

        // Override the default 1-second reconnect delay to keep the test fast by using
        // a watcher whose _cts we will cancel, not by waiting 1 s
        using var watcher = new MongoDBWatcher(clientFactory, databaseNameResolver, logger);

        // Trigger watching (EnsureWatching is called on first Observe call)
        _ = watcher.Observe<BsonDocument>();

        // Wait for the second WatchAsync call to start — proves the loop retried
        await secondCallStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));

        callCount.ShouldBeGreaterThan(1);
    }
}
