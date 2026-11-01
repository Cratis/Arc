// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;

namespace Cratis.Arc.MongoDB.for_MongoDBClientFactory.when_resilience_is_enabled;

public class with_a_collection_not_found_value_task : given.a_client_with_resilience_enabled
{
    long _result;

    void Establish() => _nativeCollection.CountDocumentsAsync(Arg.Any<FilterDefinition<given.Document>>(), Arg.Any<CountOptions>(), Arg.Any<CancellationToken>()).Returns(Task.FromException<long>(_collectionNotFound));

    async Task Because() => _result = await _collection.CountDocumentsAsync(FilterDefinition<given.Document>.Empty);

    [Fact] void should_return_the_default_value() => _result.ShouldEqual(0L);
    [Fact] void should_call_the_driver_once() => _nativeCollection.Received(1).CountDocumentsAsync(Arg.Any<FilterDefinition<given.Document>>(), Arg.Any<CountOptions>(), Arg.Any<CancellationToken>());
}
