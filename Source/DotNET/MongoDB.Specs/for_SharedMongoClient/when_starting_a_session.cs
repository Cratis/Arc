// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;

namespace Cratis.Arc.MongoDB.for_SharedMongoClient;

public class when_starting_a_session : given.a_shared_client
{
    readonly ClientSessionOptions _options = new();
    Task<IClientSessionHandle> _task;
    IClientSessionHandle _result;
    Task<IClientSessionHandle> _asyncResult;

    void Establish()
    {
        _task = Task.FromResult(_session);
        _native.StartSession(_options, _token).Returns(_session);
        _native.StartSessionAsync(_options, _token).Returns(_task);
    }

    void Because()
    {
        _result = _client.StartSession(_options, _token);
        _asyncResult = _client.StartSessionAsync(_options, _token);
    }

    [Fact] void should_forward_the_sync_call() => _native.Received(1).StartSession(_options, _token);
    [Fact] void should_forward_the_async_call() => _native.Received(1).StartSessionAsync(_options, _token);
    [Fact] void should_return_the_native_session() => _result.ShouldBeSame(_session);
    [Fact] void should_return_the_native_task() => _asyncResult.ShouldBeSame(_task);
}
