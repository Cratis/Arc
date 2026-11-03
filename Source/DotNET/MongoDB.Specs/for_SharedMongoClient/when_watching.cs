// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Arc.MongoDB.for_SharedMongoClient;

public class when_watching : given.a_shared_client
{
    readonly PipelineDefinition<ChangeStreamDocument<BsonDocument>, BsonDocument> _pipeline = new BsonDocument[] { new("$project", new BsonDocument("fullDocument", 1)) };
    readonly ChangeStreamOptions _options = new();
    IChangeStreamCursor<BsonDocument> _cursor;
    Task<IChangeStreamCursor<BsonDocument>> _task;
    IChangeStreamCursor<BsonDocument> _result;
    IChangeStreamCursor<BsonDocument> _sessionResult;
    Task<IChangeStreamCursor<BsonDocument>> _asyncResult;
    Task<IChangeStreamCursor<BsonDocument>> _asyncSessionResult;

    void Establish()
    {
        _cursor = Substitute.For<IChangeStreamCursor<BsonDocument>>();
        _task = Task.FromResult(_cursor);
        _native.Watch(_pipeline, _options, _token).Returns(_cursor);
        _native.Watch(_session, _pipeline, _options, _token).Returns(_cursor);
        _native.WatchAsync(_pipeline, _options, _token).Returns(_task);
        _native.WatchAsync(_session, _pipeline, _options, _token).Returns(_task);
    }

    void Because()
    {
        _result = _client.Watch(_pipeline, _options, _token);
        _sessionResult = _client.Watch(_session, _pipeline, _options, _token);
        _asyncResult = _client.WatchAsync(_pipeline, _options, _token);
        _asyncSessionResult = _client.WatchAsync(_session, _pipeline, _options, _token);
    }

    [Fact] void should_forward_the_sync_call() => _native.Received(1).Watch(_pipeline, _options, _token);
    [Fact] void should_forward_the_sync_session_call() => _native.Received(1).Watch(_session, _pipeline, _options, _token);
    [Fact] void should_forward_the_async_call() => _native.Received(1).WatchAsync(_pipeline, _options, _token);
    [Fact] void should_forward_the_async_session_call() => _native.Received(1).WatchAsync(_session, _pipeline, _options, _token);
    [Fact] void should_return_the_native_cursor() => _result.ShouldBeSame(_cursor);
    [Fact] void should_return_the_native_session_cursor() => _sessionResult.ShouldBeSame(_cursor);
    [Fact] void should_return_the_native_task() => _asyncResult.ShouldBeSame(_task);
    [Fact] void should_return_the_native_session_task() => _asyncSessionResult.ShouldBeSame(_task);
}
