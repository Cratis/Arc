// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Arc.MongoDB.for_MongoDBWatcher.given;

public class watched_databases : Specification
{
    protected const string DatabaseName = "jobs";
    protected static readonly TimeSpan Deadline = TimeSpan.FromSeconds(5);
    protected IMongoDBClientFactory _clientFactory;
    protected MongoDBWatcher _watcher;

    void Establish()
    {
        _clientFactory = Substitute.For<IMongoDBClientFactory>();
        _watcher = new MongoDBWatcher(_clientFactory, Substitute.For<IMongoDatabaseNameResolver>(), Substitute.For<Microsoft.Extensions.Logging.ILogger<MongoDBWatcher>>());
    }

    void Destroy() => _watcher.Dispose();

    /// <summary>
    /// Creates a database on a client for the given servers, whose change stream signals when it is opened and then blocks.
    /// </summary>
    /// <param name="servers">The servers the client connects to.</param>
    /// <returns>The client, its database, and a task that completes when the database's change stream is opened.</returns>
    protected static (IMongoClient Client, IMongoDatabase Database, Task WatchStarted) DatabaseOn(string servers)
    {
        var watchStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var cursor = Substitute.For<IChangeStreamCursor<ChangeStreamDocument<BsonDocument>>>();
        var block = new TaskCompletionSource<bool>();
        cursor.MoveNextAsync(Arg.Any<CancellationToken>()).Returns(_ =>
        {
            watchStarted.TrySetResult();
            return block.Task;
        });

        var client = Substitute.For<IMongoClient>();
        client.Settings.Returns(MongoClientSettings.FromConnectionString($"mongodb://{servers}"));
        var database = Substitute.For<IMongoDatabase>();
        database.DatabaseNamespace.Returns(new DatabaseNamespace(DatabaseName));
        database.Client.Returns(client);
        database.WatchAsync(
            Arg.Any<PipelineDefinition<ChangeStreamDocument<BsonDocument>, ChangeStreamDocument<BsonDocument>>>(),
            Arg.Any<ChangeStreamOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(cursor));
        client.GetDatabase(DatabaseName, Arg.Any<MongoDatabaseSettings>()).Returns(database);
        return (client, database, watchStarted.Task);
    }
}
