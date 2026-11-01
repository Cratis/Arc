// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.MongoDB.Resilience;
using MongoDB.Driver;

namespace Cratis.Arc.MongoDB.for_MongoDBClientFactory.when_resilience_is_enabled;

public class with_a_collection_not_found_change_stream_task : given.a_client_with_resilience_enabled
{
    IChangeStreamCursor<ChangeStreamDocument<given.Document>> _result;

    void Establish() => _nativeCollection.WatchAsync(Arg.Any<PipelineDefinition<ChangeStreamDocument<given.Document>, ChangeStreamDocument<given.Document>>>(), Arg.Any<ChangeStreamOptions>(), Arg.Any<CancellationToken>()).Returns(Task.FromException<IChangeStreamCursor<ChangeStreamDocument<given.Document>>>(_collectionNotFound));

    async Task Because() => _result = await _collection.WatchAsync();

    [Fact] void should_return_a_retrying_cursor() => _result.ShouldBeOfExactType<RetryingChangeStreamCursor<ChangeStreamDocument<given.Document>>>();
    [Fact] void should_not_retry_until_the_cursor_is_moved() => _nativeCollection.Received(1).WatchAsync(Arg.Any<PipelineDefinition<ChangeStreamDocument<given.Document>, ChangeStreamDocument<given.Document>>>(), Arg.Any<ChangeStreamOptions>(), Arg.Any<CancellationToken>());
}
