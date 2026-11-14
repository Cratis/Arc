// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Arc.MongoDB.for_MongoDBChangeStreamSupport;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Arc.MongoDB.for_MongoDBWatcher.when_watching;

public class and_change_streams_are_not_supported : Specification
{
    const string DatabaseName = "unsupported";
    IMongoDatabase _database;
    MongoDBWatcher _watcher;
    MongoCommandException _driverFailure;
    TaskCompletionSource<IChangeStreamCursor<ChangeStreamDocument<BsonDocument>>> _refusal;
    IObservable<ChangeStreamDocument<BsonDocument>>[] _watches;
    IObservable<ChangeStreamDocument<BsonDocument>> _lateWatch;
    ISubject<int> _subject;
    IDisposable _subscription;
    MongoDBChangeStreamsNotSupported _firstError;
    MongoDBChangeStreamsNotSupported _lateError;
    Exception _observableError;
    int _queries;

    void Establish()
    {
        _driverFailure = when_classifying_driver_failures.Error(40573, "The $changeStream stage is only supported on replica sets");
        _refusal = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _database = Substitute.For<IMongoDatabase>();
        _database.WatchAsync(
            Arg.Any<PipelineDefinition<ChangeStreamDocument<BsonDocument>, ChangeStreamDocument<BsonDocument>>>(),
            Arg.Any<ChangeStreamOptions>(),
            Arg.Any<CancellationToken>()).Returns(_ => _refusal.Task);
        var client = Substitute.For<IMongoClient>();
        client.GetDatabase(DatabaseName).Returns(_database);
        var supportedDatabase = SupportedDatabase();
        client.GetDatabase("supported").Returns(supportedDatabase);
        var factory = Substitute.For<IMongoDBClientFactory>();
        factory.Create().Returns(client);
        _watcher = new(factory, Substitute.For<IMongoDatabaseNameResolver>(), NullLogger<MongoDBWatcher>.Instance);
    }

    async Task Because()
    {
        _watches = await Task.WhenAll(Enumerable.Range(0, 16).Select(_ => Task.Run(() => _watcher.GetChanges(DatabaseName))));
        var opening = ((MongoDBDatabaseWatch)_watches[0]).WhenOpen(CancellationToken.None);
        var observedError = Observe(_watches[0]);
        _refusal.SetException(_driverFailure);
        _firstError = await Failure(opening);
        _observableError = await observedError.WaitAsync(TimeSpan.FromSeconds(5));

        _lateWatch = _watcher.GetChanges(DatabaseName);
        _lateError = await Failure(((MongoDBDatabaseWatch)_lateWatch).WhenOpen(CancellationToken.None));
        var supported = (MongoDBDatabaseWatch)_watcher.GetChanges("supported");
        await supported.WhenOpen(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(5));
    }

    void Destroy()
    {
        _subscription.Dispose();
        (_subject as IDisposable)?.Dispose();
        _watcher.Dispose();
    }

    [Fact] void should_share_one_watch_across_concurrent_requests() => _watches.ShouldEachConformTo(watch => ReferenceEquals(watch, _watches[0]));
    [Fact] void should_reuse_the_failed_database_watch() => Assert.Same(_watches[0], _lateWatch);
    [Fact] void should_retain_the_failure_for_later_subscriptions() => Assert.Same(_firstError, _lateError);
    [Fact] void should_report_the_capability_failure_to_the_observable() => Assert.Same(_firstError, _observableError);
    [Fact] void should_not_query_a_snapshot_without_change_streams() => _queries.ShouldEqual(0);
    [Fact] void should_preserve_the_driver_failure() => Assert.Same(_driverFailure, _firstError.InnerException);
    [Fact] void should_explain_the_requirement() => Assert.Contains("change streams", _firstError.Message);
    [Fact] void should_explain_how_to_enable_the_capability() => Assert.Contains("replica set or a compatible service", _firstError.Message);

    [Fact]
    async Task should_attempt_the_database_watch_only_once() => await _database.Received(1).WatchAsync(
        Arg.Any<PipelineDefinition<ChangeStreamDocument<BsonDocument>, ChangeStreamDocument<BsonDocument>>>(),
        Arg.Any<ChangeStreamOptions>(),
        Arg.Any<CancellationToken>());

    Task<Exception> Observe(IObservable<ChangeStreamDocument<BsonDocument>> watch)
    {
        var error = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);
        _subject = MongoDBObservation.Create(
            watch,
            new HashSet<CollectionNamespace> { new(DatabaseName, "items") },
            _ => Task.FromResult(Interlocked.Increment(ref _queries)),
            value => value,
            NullLogger<MongoDBWatcher>.Instance);
        _subscription = _subject.Subscribe(_ => { }, failure => error.TrySetResult(failure));
        return error.Task;
    }

    static Task<MongoDBChangeStreamsNotSupported> Failure(Task opening) =>
        Assert.ThrowsAsync<MongoDBChangeStreamsNotSupported>(() => opening.WaitAsync(TimeSpan.FromSeconds(5)));

    static IMongoDatabase SupportedDatabase()
    {
        var database = Substitute.For<IMongoDatabase>();
        var cursor = Substitute.For<IChangeStreamCursor<ChangeStreamDocument<BsonDocument>>>();
        var hold = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        cursor.MoveNextAsync(Arg.Any<CancellationToken>()).Returns(call => hold.Task.WaitAsync(call.Arg<CancellationToken>()));
        database.WatchAsync(
            Arg.Any<PipelineDefinition<ChangeStreamDocument<BsonDocument>, ChangeStreamDocument<BsonDocument>>>(),
            Arg.Any<ChangeStreamOptions>(),
            Arg.Any<CancellationToken>()).Returns(Task.FromResult(cursor));
        return database;
    }
}
