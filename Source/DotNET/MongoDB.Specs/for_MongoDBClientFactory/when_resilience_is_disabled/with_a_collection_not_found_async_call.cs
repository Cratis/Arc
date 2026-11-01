// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;

namespace Cratis.Arc.MongoDB.for_MongoDBClientFactory.when_resilience_is_disabled;

public class with_a_collection_not_found_async_call : given.a_client_with_resilience_disabled
{
    MongoCommandException _expected;
    Exception[] _errors;

    void Establish()
    {
        _expected = _collectionNotFound;
        _nativeCollection.InsertOneAsync(Arg.Any<given.Document>(), Arg.Any<InsertOneOptions>(), Arg.Any<CancellationToken>()).Returns(Task.FromException(_expected));
        _nativeCollection.CountDocumentsAsync(Arg.Any<FilterDefinition<given.Document>>(), Arg.Any<CountOptions>(), Arg.Any<CancellationToken>()).Returns(Task.FromException<long>(_expected));
        _nativeCollection.FindAsync(Arg.Any<FilterDefinition<given.Document>>(), Arg.Any<FindOptions<given.Document, given.Document>>(), Arg.Any<CancellationToken>()).Returns(Task.FromException<IAsyncCursor<given.Document>>(_expected));
        _nativeCollection.WatchAsync(Arg.Any<PipelineDefinition<ChangeStreamDocument<given.Document>, ChangeStreamDocument<given.Document>>>(), Arg.Any<ChangeStreamOptions>(), Arg.Any<CancellationToken>()).Returns(Task.FromException<IChangeStreamCursor<ChangeStreamDocument<given.Document>>>(_expected));
    }

    async Task Because()
    {
        _errors =
        [
            await Catch.Exception(async () => await _collection.InsertOneAsync(new given.Document(Guid.NewGuid()))),
            await Catch.Exception(async () => await _collection.CountDocumentsAsync(FilterDefinition<given.Document>.Empty)),
            await Catch.Exception(async () => await _collection.FindAsync(FilterDefinition<given.Document>.Empty)),
            await Catch.Exception(async () => await _collection.WatchAsync())
        ];
    }

    [Fact] void should_preserve_all_native_exceptions() => _errors.ShouldEachConformTo(error => ReferenceEquals(error, _expected));
    [Fact] void should_not_retry_the_void_task() => _nativeCollection.Received(1).InsertOneAsync(Arg.Any<given.Document>(), Arg.Any<InsertOneOptions>(), Arg.Any<CancellationToken>());
    [Fact] void should_not_retry_the_value_task() => _nativeCollection.Received(1).CountDocumentsAsync(Arg.Any<FilterDefinition<given.Document>>(), Arg.Any<CountOptions>(), Arg.Any<CancellationToken>());
    [Fact] void should_not_retry_the_cursor_task() => _nativeCollection.Received(1).FindAsync(Arg.Any<FilterDefinition<given.Document>>(), Arg.Any<FindOptions<given.Document, given.Document>>(), Arg.Any<CancellationToken>());
    [Fact] void should_not_retry_the_change_stream_task() => _nativeCollection.Received(1).WatchAsync(Arg.Any<PipelineDefinition<ChangeStreamDocument<given.Document>, ChangeStreamDocument<given.Document>>>(), Arg.Any<ChangeStreamOptions>(), Arg.Any<CancellationToken>());
}
