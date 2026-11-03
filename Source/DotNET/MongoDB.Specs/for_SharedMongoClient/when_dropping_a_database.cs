// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.MongoDB.for_SharedMongoClient;

public class when_dropping_a_database : given.a_shared_client
{
    readonly Task _task = Task.FromResult(42);
    Task _result;
    Task _sessionResult;

    void Establish()
    {
        _native.DropDatabaseAsync("db", _token).Returns(_task);
        _native.DropDatabaseAsync(_session, "db", _token).Returns(_task);
    }

    void Because()
    {
        _client.DropDatabase("db", _token);
        _client.DropDatabase(_session, "db", _token);
        _result = _client.DropDatabaseAsync("db", _token);
        _sessionResult = _client.DropDatabaseAsync(_session, "db", _token);
    }

    [Fact] void should_forward_the_sync_call() => _native.Received(1).DropDatabase("db", _token);
    [Fact] void should_forward_the_sync_session_call() => _native.Received(1).DropDatabase(_session, "db", _token);
    [Fact] void should_forward_the_async_call() => _native.Received(1).DropDatabaseAsync("db", _token);
    [Fact] void should_forward_the_async_session_call() => _native.Received(1).DropDatabaseAsync(_session, "db", _token);
    [Fact] void should_return_the_native_task() => _result.ShouldBeSame(_task);
    [Fact] void should_return_the_native_session_task() => _sessionResult.ShouldBeSame(_task);
}
