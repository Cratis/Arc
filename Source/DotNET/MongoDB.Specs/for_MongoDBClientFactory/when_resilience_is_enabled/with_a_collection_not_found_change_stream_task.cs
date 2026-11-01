// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.MongoDB.Resilience;
using MongoDB.Driver;

namespace Cratis.Arc.MongoDB.for_MongoDBClientFactory.when_resilience_is_enabled;

public class with_a_collection_not_found_change_stream_task : given.a_client_with_resilience_enabled
{
    IChangeStreamCursor<ChangeStreamDocument<given.Document>> _nativeCursor;
    IChangeStreamCursor<ChangeStreamDocument<given.Document>> _result;
    int _watchCalls;
    int _callsBeforeMoving;
    bool _moved;

    void Establish()
    {
        _nativeCursor = Substitute.For<IChangeStreamCursor<ChangeStreamDocument<given.Document>>>();
        _nativeCursor.MoveNextAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(true));
        _nativeCollection.WatchAsync(Arg.Any<PipelineDefinition<ChangeStreamDocument<given.Document>, ChangeStreamDocument<given.Document>>>(), Arg.Any<ChangeStreamOptions>(), Arg.Any<CancellationToken>()).Returns(_ => ++_watchCalls <= 2
            ? Task.FromException<IChangeStreamCursor<ChangeStreamDocument<given.Document>>>(_collectionNotFound)
            : Task.FromResult(_nativeCursor));
    }

    async Task Because()
    {
        var watch = _collection.WatchAsync();
        await CompleteWithinDeadline(watch);
        _result = await watch;
        _callsBeforeMoving = _watchCalls;
        var move = _result.MoveNextAsync();
        await CompleteWithinDeadline(move);
        _moved = await move;
    }

    void Destroy() => _result?.Dispose();

    [Fact] void should_return_a_retrying_cursor() => _result.ShouldBeOfExactType<RetryingChangeStreamCursor<ChangeStreamDocument<given.Document>>>();
    [Fact] void should_not_retry_before_the_cursor_is_moved() => _callsBeforeMoving.ShouldEqual(1);
    [Fact] void should_wait_until_the_collection_exists() => _moved.ShouldBeTrue();
    [Fact] void should_retry_opening_the_native_cursor() => _nativeCollection.Received(3).WatchAsync(Arg.Any<PipelineDefinition<ChangeStreamDocument<given.Document>, ChangeStreamDocument<given.Document>>>(), Arg.Any<ChangeStreamOptions>(), Arg.Any<CancellationToken>());
    [Fact] void should_move_the_native_cursor() => _nativeCursor.Received(1).MoveNextAsync(Arg.Any<CancellationToken>());
}
