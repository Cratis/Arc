// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;

namespace Cratis.Arc.MongoDB.for_MongoDBClientFactory.when_resilience_is_enabled;

public class with_a_synchronous_collection_not_found_call : given.a_client_with_resilience_enabled
{
    Exception _error;

    void Establish() => _nativeCollection.CountDocuments(Arg.Any<FilterDefinition<given.Document>>(), Arg.Any<CountOptions>(), Arg.Any<CancellationToken>()).Returns(_ => throw _collectionNotFound);

    void Because() => _error = Catch.Exception(() => _collection.CountDocuments(FilterDefinition<given.Document>.Empty));

    [Fact] void should_not_swallow_synchronous_errors() => _error.ShouldBeSame(_collectionNotFound);
    [Fact] void should_call_the_driver_once() => _nativeCollection.Received(1).CountDocuments(Arg.Any<FilterDefinition<given.Document>>(), Arg.Any<CountOptions>(), Arg.Any<CancellationToken>());
}
