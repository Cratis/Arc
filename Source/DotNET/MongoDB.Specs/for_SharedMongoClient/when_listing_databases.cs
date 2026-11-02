// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Arc.MongoDB.for_SharedMongoClient;

public class when_listing_databases : given.a_shared_client
{
    readonly ListDatabasesOptions _options = new();
    IAsyncCursor<BsonDocument> _cursor;
    Task<IAsyncCursor<BsonDocument>> _task;
    IAsyncCursor<BsonDocument>[] _results;
    Task<IAsyncCursor<BsonDocument>>[] _tasks;

    void Establish()
    {
        _cursor = Substitute.For<IAsyncCursor<BsonDocument>>();
        _task = Task.FromResult(_cursor);
        _native.ListDatabases(_token).Returns(_cursor);
        _native.ListDatabases(_options, _token).Returns(_cursor);
        _native.ListDatabases(_session, _token).Returns(_cursor);
        _native.ListDatabases(_session, _options, _token).Returns(_cursor);
        _native.ListDatabasesAsync(_token).Returns(_task);
        _native.ListDatabasesAsync(_options, _token).Returns(_task);
        _native.ListDatabasesAsync(_session, _token).Returns(_task);
        _native.ListDatabasesAsync(_session, _options, _token).Returns(_task);
    }

    void Because()
    {
        _results = [_client.ListDatabases(_token), _client.ListDatabases(_options, _token), _client.ListDatabases(_session, _token), _client.ListDatabases(_session, _options, _token)];
        _tasks = [_client.ListDatabasesAsync(_token), _client.ListDatabasesAsync(_options, _token), _client.ListDatabasesAsync(_session, _token), _client.ListDatabasesAsync(_session, _options, _token)];
    }

    [Fact] void should_forward_the_sync_call() => _native.Received(1).ListDatabases(_token);
    [Fact] void should_forward_the_sync_options_call() => _native.Received(1).ListDatabases(_options, _token);
    [Fact] void should_forward_the_sync_session_call() => _native.Received(1).ListDatabases(_session, _token);
    [Fact] void should_forward_the_sync_session_options_call() => _native.Received(1).ListDatabases(_session, _options, _token);
    [Fact] void should_forward_the_async_call() => _native.Received(1).ListDatabasesAsync(_token);
    [Fact] void should_forward_the_async_options_call() => _native.Received(1).ListDatabasesAsync(_options, _token);
    [Fact] void should_forward_the_async_session_call() => _native.Received(1).ListDatabasesAsync(_session, _token);
    [Fact] void should_forward_the_async_session_options_call() => _native.Received(1).ListDatabasesAsync(_session, _options, _token);
    [Fact] void should_return_the_native_cursors() => _results.ShouldEachConformTo(_ => ReferenceEquals(_, _cursor));
    [Fact] void should_return_the_native_tasks() => _tasks.ShouldEachConformTo(_ => ReferenceEquals(_, _task));
}
