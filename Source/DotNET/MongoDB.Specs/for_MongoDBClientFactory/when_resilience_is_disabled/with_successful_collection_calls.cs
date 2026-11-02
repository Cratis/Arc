// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;

namespace Cratis.Arc.MongoDB.for_MongoDBClientFactory.when_resilience_is_disabled;

public class with_successful_collection_calls : given.a_client_with_resilience_disabled
{
    readonly given.Document _document = new(Guid.NewGuid());
    readonly Task _insertTask = Task.FromResult(42);
    readonly Task<long> _countTask = Task.FromResult(17L);
    Task _insertResult;
    Task<long> _countResult;

    void Establish()
    {
        _nativeCollection.InsertOneAsync(_document, Arg.Any<InsertOneOptions>(), Arg.Any<CancellationToken>()).Returns(_insertTask);
        _nativeCollection.CountDocumentsAsync(Arg.Any<FilterDefinition<given.Document>>(), Arg.Any<CountOptions>(), Arg.Any<CancellationToken>()).Returns(_countTask);
    }

    void Because()
    {
        _insertResult = _collection.InsertOneAsync(_document);
        _countResult = _collection.CountDocumentsAsync(FilterDefinition<given.Document>.Empty);
    }

    [Fact] void should_return_a_shared_client() => _client.ShouldBeOfExactType<SharedMongoClient>();
    [Fact] void should_return_the_native_database() => _client.GetDatabase("db").ShouldBeSame(_nativeDatabase);
    [Fact] void should_return_the_native_collection() => _collection.ShouldBeSame(_nativeCollection);
    [Fact] void should_return_the_native_void_task() => _insertResult.ShouldBeSame(_insertTask);
    [Fact] void should_return_the_native_generic_task() => _countResult.ShouldBeSame(_countTask);
    [Fact] void should_forward_the_void_task_call_once() => _nativeCollection.Received(1).InsertOneAsync(_document, Arg.Any<InsertOneOptions>(), Arg.Any<CancellationToken>());
    [Fact] void should_forward_the_generic_task_call_once() => _nativeCollection.Received(1).CountDocumentsAsync(Arg.Any<FilterDefinition<given.Document>>(), Arg.Any<CountOptions>(), Arg.Any<CancellationToken>());
}
