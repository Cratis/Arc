// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

namespace Cratis.Arc.MongoDB.for_MongoDBDatabaseWatch.given;

public class a_database_watch : Specification
{
    protected IMongoDatabase _database;
    protected IChangeStreamCursor<ChangeStreamDocument<BsonDocument>> _firstCursor;
    protected IChangeStreamCursor<ChangeStreamDocument<BsonDocument>> _secondCursor;
    private protected MongoDBDatabaseWatch _watch;
    protected CancellationTokenSource _cts;
    protected Task _worker = Task.CompletedTask;
    protected TaskCompletionSource<bool> _nextMove;
    protected TaskCompletionSource _delivered;
    protected TaskCompletionSource _resynchronized;
    protected List<ChangeStreamOptions> _options;
    protected BsonDocument _resumeToken;
    IDisposable _subscription;

    void Establish()
    {
        _cts = new();
        _options = [];
        _resumeToken = new BsonDocument("_data", "old-token");
        _nextMove = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _delivered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _resynchronized = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _database = Substitute.For<IMongoDatabase>();
        _firstCursor = Substitute.For<IChangeStreamCursor<ChangeStreamDocument<BsonDocument>>>();
        _firstCursor.Current.Returns([new ChangeStreamDocument<BsonDocument>(
            new BsonDocument
            {
                { "_id", _resumeToken },
                { "operationType", "insert" },
                { "ns", new BsonDocument { { "db", "testdb" }, { "coll", "items" } } }
            },
            BsonDocumentSerializer.Instance)]);
        var moves = 0;
        _firstCursor.MoveNextAsync(Arg.Any<CancellationToken>()).Returns(call =>
            ++moves == 1 ? Task.FromResult(true) : _nextMove.Task.WaitAsync(call.Arg<CancellationToken>()));
        _secondCursor = Substitute.For<IChangeStreamCursor<ChangeStreamDocument<BsonDocument>>>();
        var hold = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        _secondCursor.MoveNextAsync(Arg.Any<CancellationToken>()).Returns(call => hold.Task.WaitAsync(call.Arg<CancellationToken>()));
        _database.WatchAsync(
            Arg.Any<PipelineDefinition<ChangeStreamDocument<BsonDocument>, ChangeStreamDocument<BsonDocument>>>(),
            Arg.Do<ChangeStreamOptions>(_options.Add),
            Arg.Any<CancellationToken>()).Returns(_ => Task.FromResult(_options.Count == 1 ? _firstCursor : _secondCursor));
        _watch = new(_database, NullLogger<MongoDBWatcher>.Instance, _cts.Token);
        _subscription = _watch.Changes.Subscribe(change =>
        {
            if (change.OperationType == ChangeStreamOperationType.Invalidate)
            {
                _resynchronized.TrySetResult();
            }
            else
            {
                _delivered.TrySetResult();
            }
        });
    }

    async Task Destroy()
    {
        await _cts.CancelAsync();
        await _worker.WaitAsync(TimeSpan.FromSeconds(5));
        _subscription.Dispose();
        _cts.Dispose();
    }
}
