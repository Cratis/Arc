// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;

namespace Cratis.Arc.MongoDB.for_SharedMongoClient;

public class when_listing_database_names : given.a_shared_client
{
    readonly ListDatabaseNamesOptions _options = new();
    IAsyncCursor<string> _cursor;
    Task<IAsyncCursor<string>> _task;
    IAsyncCursor<string>[] _results;
    Task<IAsyncCursor<string>>[] _tasks;

    void Establish()
    {
        _cursor = Substitute.For<IAsyncCursor<string>>();
        _task = Task.FromResult(_cursor);
        _native.ListDatabaseNames(_token).Returns(_cursor);
        _native.ListDatabaseNames(_options, _token).Returns(_cursor);
        _native.ListDatabaseNames(_session, _token).Returns(_cursor);
        _native.ListDatabaseNames(_session, _options, _token).Returns(_cursor);
        _native.ListDatabaseNamesAsync(_token).Returns(_task);
        _native.ListDatabaseNamesAsync(_options, _token).Returns(_task);
        _native.ListDatabaseNamesAsync(_session, _token).Returns(_task);
        _native.ListDatabaseNamesAsync(_session, _options, _token).Returns(_task);
    }

    void Because()
    {
        _results = [_client.ListDatabaseNames(_token), _client.ListDatabaseNames(_options, _token), _client.ListDatabaseNames(_session, _token), _client.ListDatabaseNames(_session, _options, _token)];
        _tasks = [_client.ListDatabaseNamesAsync(_token), _client.ListDatabaseNamesAsync(_options, _token), _client.ListDatabaseNamesAsync(_session, _token), _client.ListDatabaseNamesAsync(_session, _options, _token)];
    }

    [Fact] void should_forward_the_sync_call() => _native.Received(1).ListDatabaseNames(_token);
    [Fact] void should_forward_the_sync_options_call() => _native.Received(1).ListDatabaseNames(_options, _token);
    [Fact] void should_forward_the_sync_session_call() => _native.Received(1).ListDatabaseNames(_session, _token);
    [Fact] void should_forward_the_sync_session_options_call() => _native.Received(1).ListDatabaseNames(_session, _options, _token);
    [Fact] void should_forward_the_async_call() => _native.Received(1).ListDatabaseNamesAsync(_token);
    [Fact] void should_forward_the_async_options_call() => _native.Received(1).ListDatabaseNamesAsync(_options, _token);
    [Fact] void should_forward_the_async_session_call() => _native.Received(1).ListDatabaseNamesAsync(_session, _token);
    [Fact] void should_forward_the_async_session_options_call() => _native.Received(1).ListDatabaseNamesAsync(_session, _options, _token);
    [Fact] void should_return_the_native_cursors() => _results.ShouldEachConformTo(_ => ReferenceEquals(_, _cursor));
    [Fact] void should_return_the_native_tasks() => _tasks.ShouldEachConformTo(_ => ReferenceEquals(_, _task));
}
