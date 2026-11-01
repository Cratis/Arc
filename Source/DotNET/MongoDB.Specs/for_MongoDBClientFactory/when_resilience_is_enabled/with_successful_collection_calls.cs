// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Castle.DynamicProxy;
using MongoDB.Driver;

namespace Cratis.Arc.MongoDB.for_MongoDBClientFactory.when_resilience_is_enabled;

public class with_successful_collection_calls : given.a_client_with_resilience_enabled
{
    readonly given.Document _document = new(Guid.NewGuid());
    long _count;

    void Establish() => _nativeCollection.CountDocumentsAsync(Arg.Any<FilterDefinition<given.Document>>(), Arg.Any<CountOptions>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(17L));

    async Task Because()
    {
        await _collection.InsertOneAsync(_document);
        _count = await _collection.CountDocumentsAsync(FilterDefinition<given.Document>.Empty);
    }

    [Fact] void should_keep_the_root_proxy() => ((IProxyTargetAccessor)_client).DynProxyGetTarget().ShouldBeSame(_native);
    [Fact] void should_keep_the_database_proxy() => ((IProxyTargetAccessor)_client.GetDatabase("db")).DynProxyGetTarget().ShouldBeSame(_nativeDatabase);
    [Fact] void should_keep_the_collection_proxy() => ((IProxyTargetAccessor)_collection).DynProxyGetTarget().ShouldBeSame(_nativeCollection);
    [Fact] void should_forward_the_void_task_call() => _nativeCollection.Received(1).InsertOneAsync(_document, Arg.Any<InsertOneOptions>(), Arg.Any<CancellationToken>());
    [Fact] void should_forward_the_generic_task_call() => _nativeCollection.Received(1).CountDocumentsAsync(Arg.Any<FilterDefinition<given.Document>>(), Arg.Any<CountOptions>(), Arg.Any<CancellationToken>());
    [Fact] void should_return_the_native_value() => _count.ShouldEqual(17L);
}
