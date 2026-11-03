// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;

namespace Cratis.Arc.MongoDB.for_MongoDBClientFactory.when_resilience_is_enabled;

public class with_a_collection_not_found_cursor_task : given.a_client_with_resilience_enabled
{
    IAsyncCursor<given.Document> _result;
    bool _moved;

    void Establish() => _nativeCollection.FindAsync(Arg.Any<FilterDefinition<given.Document>>(), Arg.Any<FindOptions<given.Document, given.Document>>(), Arg.Any<CancellationToken>()).Returns(Task.FromException<IAsyncCursor<given.Document>>(_collectionNotFound));

    async Task Because()
    {
        _result = await _collection.FindAsync(FilterDefinition<given.Document>.Empty);
        _moved = await _result.MoveNextAsync();
    }

    [Fact] void should_return_an_empty_cursor() => _moved.ShouldBeFalse();
    [Fact] void should_have_no_documents() => _result.Current.ShouldBeEmpty();
    [Fact] void should_call_the_driver_once() => _nativeCollection.Received(1).FindAsync(Arg.Any<FilterDefinition<given.Document>>(), Arg.Any<FindOptions<given.Document, given.Document>>(), Arg.Any<CancellationToken>());
}
