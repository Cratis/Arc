// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;

namespace Cratis.Arc.MongoDB.Resilience.for_ResilientMongoCollection.when_the_collection_does_not_exist;

public class and_watching_it : given.a_collection_the_server_reports_as_missing
{
    IChangeStreamCursor<ChangeStreamDocument<string>> _result;

    void Establish() =>
        _inner.WatchAsync(Arg.Any<PipelineDefinition<ChangeStreamDocument<string>, ChangeStreamDocument<string>>>(), Arg.Any<ChangeStreamOptions>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<IChangeStreamCursor<ChangeStreamDocument<string>>>(CollectionNotFound()));

    async Task Because() => _result = await _collection.WatchAsync(new EmptyPipelineDefinition<ChangeStreamDocument<string>>(), null!, CancellationToken.None);

    [Fact] void should_watch_until_the_collection_exists() => _result.ShouldBeOfExactType<RetryingChangeStreamCursor<ChangeStreamDocument<string>>>();
}
