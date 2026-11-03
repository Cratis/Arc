// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;

namespace Cratis.Arc.MongoDB.for_MongoDBClientFactory.when_resilience_is_disabled;

public class watching_a_collection : given.a_client_with_resilience_disabled
{
    IChangeStreamCursor<ChangeStreamDocument<given.Document>> _nativeCursor;
    Task<IChangeStreamCursor<ChangeStreamDocument<given.Document>>> _nativeTask;
    Task<IChangeStreamCursor<ChangeStreamDocument<given.Document>>> _result;
    IChangeStreamCursor<ChangeStreamDocument<given.Document>> _cursor;

    void Establish()
    {
        _nativeCursor = Substitute.For<IChangeStreamCursor<ChangeStreamDocument<given.Document>>>();
        _nativeTask = Task.FromResult(_nativeCursor);
        _nativeCollection.WatchAsync(Arg.Any<PipelineDefinition<ChangeStreamDocument<given.Document>, ChangeStreamDocument<given.Document>>>(), Arg.Any<ChangeStreamOptions>(), Arg.Any<CancellationToken>()).Returns(_nativeTask);
    }

    async Task Because()
    {
        _result = _collection.WatchAsync();
        await CompleteWithinDeadline(_result);
        _cursor = await _result;
    }

    [Fact] void should_return_the_native_task() => _result.ShouldBeSame(_nativeTask);
    [Fact] void should_return_the_native_cursor_without_a_retrying_wrapper() => _cursor.ShouldBeSame(_nativeCursor);
    [Fact] void should_only_call_the_driver_once() => _nativeCollection.Received(1).WatchAsync(Arg.Any<PipelineDefinition<ChangeStreamDocument<given.Document>, ChangeStreamDocument<given.Document>>>(), Arg.Any<ChangeStreamOptions>(), Arg.Any<CancellationToken>());
}
