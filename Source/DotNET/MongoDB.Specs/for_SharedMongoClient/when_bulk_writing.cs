// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Arc.MongoDB.for_SharedMongoClient;

public class when_bulk_writing : given.a_shared_client
{
    readonly IReadOnlyList<BulkWriteModel> _models = [new BulkWriteInsertOneModel<BsonDocument>("db.docs", new())];
    readonly ClientBulkWriteOptions _options = new();
    readonly ClientBulkWriteResult _nativeResult = new();
    Task<ClientBulkWriteResult> _task;
    ClientBulkWriteResult _result;
    ClientBulkWriteResult _sessionResult;
    Task<ClientBulkWriteResult> _asyncResult;
    Task<ClientBulkWriteResult> _asyncSessionResult;

    void Establish()
    {
        _task = Task.FromResult(_nativeResult);
        _native.BulkWrite(_models, _options, _token).Returns(_nativeResult);
        _native.BulkWrite(_session, _models, _options, _token).Returns(_nativeResult);
        _native.BulkWriteAsync(_models, _options, _token).Returns(_task);
        _native.BulkWriteAsync(_session, _models, _options, _token).Returns(_task);
    }

    void Because()
    {
        _result = _client.BulkWrite(_models, _options, _token);
        _sessionResult = _client.BulkWrite(_session, _models, _options, _token);
        _asyncResult = _client.BulkWriteAsync(_models, _options, _token);
        _asyncSessionResult = _client.BulkWriteAsync(_session, _models, _options, _token);
    }

    [Fact] void should_forward_the_sync_call() => _native.Received(1).BulkWrite(_models, _options, _token);
    [Fact] void should_forward_the_sync_session_call() => _native.Received(1).BulkWrite(_session, _models, _options, _token);
    [Fact] void should_forward_the_async_call() => _native.Received(1).BulkWriteAsync(_models, _options, _token);
    [Fact] void should_forward_the_async_session_call() => _native.Received(1).BulkWriteAsync(_session, _models, _options, _token);
    [Fact] void should_return_the_native_result() => _result.ShouldBeSame(_nativeResult);
    [Fact] void should_return_the_native_session_result() => _sessionResult.ShouldBeSame(_nativeResult);
    [Fact] void should_return_the_native_task() => _asyncResult.ShouldBeSame(_task);
    [Fact] void should_return_the_native_session_task() => _asyncSessionResult.ShouldBeSame(_task);
}
