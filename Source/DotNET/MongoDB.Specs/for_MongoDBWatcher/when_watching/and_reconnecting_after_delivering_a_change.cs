// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

namespace Cratis.Arc.MongoDB.for_MongoDBWatcher.when_watching;

public class and_reconnecting_after_delivering_a_change
{
    [Fact]
    public async Task should_resume_after_the_last_delivered_change()
    {
        var namingPolicy = Substitute.For<INamingPolicy>();
        namingPolicy.GetReadModelName(Arg.Any<Type>()).Returns(ci => ci.Arg<Type>().Name.ToLowerInvariant());
        DatabaseExtensions.SetNamingPolicy(namingPolicy);

        var resumeToken = new BsonDocument("_data", "TOKEN-1");
        var change = new ChangeStreamDocument<BsonDocument>(new BsonDocument { { "_id", resumeToken } }, BsonDocumentSerializer.Instance);

        // First cursor: deliver one change (carrying the resume token), then throw a transient failure.
        var firstCursor = Substitute.For<IChangeStreamCursor<ChangeStreamDocument<BsonDocument>>>();
        var moveCount = 0;
        firstCursor.MoveNextAsync(Arg.Any<CancellationToken>()).Returns(_ =>
        {
            moveCount++;
            if (moveCount == 1)
            {
                return Task.FromResult(true);
            }

            throw new Exception("Simulated transient failure");
        });
        firstCursor.Current.Returns([change]);

        // Second cursor: block until cancelled, signalling that the reconnect happened.
        var secondWatchStarted = new TaskCompletionSource();
        var secondCursor = Substitute.For<IChangeStreamCursor<ChangeStreamDocument<BsonDocument>>>();
        var blockTcs = new TaskCompletionSource<bool>();
        secondCursor.MoveNextAsync(Arg.Any<CancellationToken>()).Returns(_ =>
        {
            secondWatchStarted.TrySetResult();
            return blockTcs.Task;
        });

        var database = Substitute.For<IMongoDatabase>();
        var capturedOptions = new List<ChangeStreamOptions>();
        var watchCount = 0;
        database.WatchAsync(
            Arg.Any<PipelineDefinition<ChangeStreamDocument<BsonDocument>, ChangeStreamDocument<BsonDocument>>>(),
            Arg.Do<ChangeStreamOptions>(capturedOptions.Add),
            Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                watchCount++;
                return Task.FromResult(watchCount == 1 ? firstCursor : secondCursor);
            });

        var collection = Substitute.For<IMongoCollection<BsonDocument>>();
        collection.CollectionNamespace.Returns(new CollectionNamespace("testdb", "bsondocument"));
        collection.Database.Returns(database);
        database.GetCollection<BsonDocument>(Arg.Any<string>(), Arg.Any<MongoCollectionSettings>()).Returns(collection);

        var mongoClient = Substitute.For<IMongoClient>();
        mongoClient.GetDatabase(Arg.Any<string>()).Returns(database);
        var clientFactory = Substitute.For<IMongoDBClientFactory>();
        clientFactory.Create().Returns(mongoClient);
        var databaseNameResolver = Substitute.For<IMongoDatabaseNameResolver>();
        databaseNameResolver.Resolve().Returns("testdb");
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<MongoDBWatcher>>();

        using var watcher = new MongoDBWatcher(clientFactory, databaseNameResolver, logger);

        _ = watcher.Observe<BsonDocument>();

        await secondWatchStarted.Task.WaitAsync(TimeSpan.FromSeconds(10));

        capturedOptions.Count.ShouldBeGreaterThan(1);
        capturedOptions[1].ResumeAfter.ShouldEqual(resumeToken);
    }
}
