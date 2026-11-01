// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;

namespace Cratis.Arc.MongoDB.for_MongoDBClientFactory.when_resilience_is_enabled;

public class with_a_collection_not_found_void_task : given.a_client_with_resilience_enabled
{
    Task _result;

    void Establish() => _nativeCollection.InsertOneAsync(Arg.Any<given.Document>(), Arg.Any<InsertOneOptions>(), Arg.Any<CancellationToken>()).Returns(Task.FromException(_collectionNotFound));

    async Task Because()
    {
        _result = _collection.InsertOneAsync(new given.Document(Guid.NewGuid()));
        await _result;
    }

    [Fact] void should_complete_successfully() => _result.IsCompletedSuccessfully.ShouldBeTrue();
    [Fact] void should_call_the_driver_once() => _nativeCollection.Received(1).InsertOneAsync(Arg.Any<given.Document>(), Arg.Any<InsertOneOptions>(), Arg.Any<CancellationToken>());
}
