// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Arc.MongoDB.for_MongoDBWatcher.when_watching;

public class and_getting_changes_for_a_database_repeatedly : Specification
{
    const string DatabaseName = "testdb";

    IMongoClient _client;
    IMongoDatabase _database;
    MongoDBWatcher _watcher;
    TaskCompletionSource _watchStarted;
    TaskCompletionSource _otherWatchStarted;
    IMongoDatabase _otherDatabase;
    IObservable<ChangeStreamDocument<BsonDocument>> _first;
    IObservable<ChangeStreamDocument<BsonDocument>> _second;
    IObservable<ChangeStreamDocument<BsonDocument>> _other;

    void Establish()
    {
        _watchStarted = new();
        _otherWatchStarted = new();
        _database = BlockingDatabase(_watchStarted);
        _otherDatabase = BlockingDatabase(_otherWatchStarted);

        _client = Substitute.For<IMongoClient>();
        _client.GetDatabase(DatabaseName).Returns(_database);
        _client.GetDatabase("otherdb").Returns(_otherDatabase);

        var clientFactory = Substitute.For<IMongoDBClientFactory>();
        clientFactory.Create().Returns(_client);

        _watcher = new MongoDBWatcher(
            clientFactory,
            Substitute.For<IMongoDatabaseNameResolver>(),
            Substitute.For<Microsoft.Extensions.Logging.ILogger<MongoDBWatcher>>());
    }

    async Task Because()
    {
        _first = _watcher.GetChanges(DatabaseName);
        _second = _watcher.GetChanges(DatabaseName);
        _other = _watcher.GetChanges("otherdb");
        await Task.WhenAll(_watchStarted.Task, _otherWatchStarted.Task).WaitAsync(TimeSpan.FromSeconds(5));
    }

    void Destroy() => _watcher.Dispose();

    [Fact] void should_share_one_stream_for_the_database() => Assert.Same(_first, _second);
    [Fact] void should_use_a_stream_of_its_own_for_another_database() => Assert.NotSame(_first, _other);
    [Fact] void should_connect_to_the_database_once() => _client.Received(1).GetDatabase(DatabaseName);

    [Fact]
    async Task should_open_one_database_level_change_stream() => await _database.Received(1).WatchAsync(
        Arg.Any<PipelineDefinition<ChangeStreamDocument<BsonDocument>, ChangeStreamDocument<BsonDocument>>>(),
        Arg.Any<ChangeStreamOptions>(),
        Arg.Any<CancellationToken>());

    static IMongoDatabase BlockingDatabase(TaskCompletionSource watchStarted)
    {
        var database = Substitute.For<IMongoDatabase>();
        var cursor = Substitute.For<IChangeStreamCursor<ChangeStreamDocument<BsonDocument>>>();
        var block = new TaskCompletionSource<bool>();
        cursor.MoveNextAsync(Arg.Any<CancellationToken>()).Returns(_ =>
        {
            watchStarted.TrySetResult();
            return block.Task;
        });

        database.WatchAsync(
            Arg.Any<PipelineDefinition<ChangeStreamDocument<BsonDocument>, ChangeStreamDocument<BsonDocument>>>(),
            Arg.Any<ChangeStreamOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(cursor));
        return database;
    }
}
