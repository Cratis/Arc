// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Serialization;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Arc.MongoDB.for_MongoDBWatcher.when_watching;

public class and_the_first_connection_attempt_fails
{
    [Fact]
    public void should_retry_on_the_next_observe_instead_of_replaying_the_cached_failure()
    {
        // Configure the global naming policy required by DatabaseExtensions.GetCollection<T>()
        var namingPolicy = Substitute.For<INamingPolicy>();
        namingPolicy.GetReadModelName(Arg.Any<Type>()).Returns(ci => ci.Arg<Type>().Name.ToLowerInvariant());
        DatabaseExtensions.SetNamingPolicy(namingPolicy);

        var database = Substitute.For<IMongoDatabase>();

        // A blocking cursor keeps the successful watch loop alive without asserting on it.
        var cursor = Substitute.For<IChangeStreamCursor<ChangeStreamDocument<BsonDocument>>>();
#pragma warning disable CA2025 // Intentional: test uses a never-completing task to keep the watch loop alive
        var blockTcs = new TaskCompletionSource<bool>();
        cursor.MoveNextAsync(Arg.Any<CancellationToken>()).Returns(_ => blockTcs.Task);
        database.WatchAsync(
            Arg.Any<PipelineDefinition<ChangeStreamDocument<BsonDocument>, ChangeStreamDocument<BsonDocument>>>(),
            Arg.Any<ChangeStreamOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(cursor));
#pragma warning restore CA2025

        var collection = Substitute.For<IMongoCollection<BsonDocument>>();
        collection.CollectionNamespace.Returns(new CollectionNamespace("testdb", "bsondocument"));
        collection.Database.Returns(database);
        database.GetCollection<BsonDocument>(Arg.Any<string>(), Arg.Any<MongoCollectionSettings>()).Returns(collection);

        var mongoClient = Substitute.For<IMongoClient>();
        mongoClient.GetDatabase(Arg.Any<string>()).Returns(database);

        // First creation throws a transient failure; the second succeeds. The old code cached the first failure in the
        // Lazy forever, so the second Observe replayed it. The evicting fix retries and succeeds.
        var clientFactory = Substitute.For<IMongoDBClientFactory>();
        clientFactory.Create().Returns(
            _ => throw new Exception("Simulated transient connection failure"),
            _ => mongoClient);

        var databaseNameResolver = Substitute.For<IMongoDatabaseNameResolver>();
        databaseNameResolver.Resolve().Returns("testdb");
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<MongoDBWatcher>>();

        using var watcher = new MongoDBWatcher(clientFactory, databaseNameResolver, logger);

        var firstError = Catch.Exception(() => watcher.Observe<BsonDocument>());
        var secondError = Catch.Exception(() => watcher.Observe<BsonDocument>());

        firstError.ShouldNotBeNull();
        secondError.ShouldBeNull();
    }
}
