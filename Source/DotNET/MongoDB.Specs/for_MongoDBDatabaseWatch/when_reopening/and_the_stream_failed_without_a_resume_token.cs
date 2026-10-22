// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Arc.MongoDB.for_MongoDBDatabaseWatch.when_reopening;

public class and_the_stream_failed_without_a_resume_token : given.a_database_watch
{
    bool _readyBeforeReopen;
    bool _resynchronizedBeforeReopen;

    async Task Because()
    {
        var reading = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _firstCursor.MoveNextAsync(Arg.Any<CancellationToken>()).Returns(call =>
        {
            reading.TrySetResult();
            return _nextMove.Task.WaitAsync(call.Arg<CancellationToken>());
        });
        var reopening = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var reopened = new TaskCompletionSource<IChangeStreamCursor<ChangeStreamDocument<BsonDocument>>>(TaskCreationOptions.RunContinuationsAsynchronously);
        _database.WatchAsync(
            Arg.Any<PipelineDefinition<ChangeStreamDocument<BsonDocument>, ChangeStreamDocument<BsonDocument>>>(),
            Arg.Any<ChangeStreamOptions>(),
            Arg.Any<CancellationToken>()).Returns(call =>
            {
                if (_options.Count == 1)
                {
                    return Task.FromResult(_firstCursor);
                }
                reopening.TrySetResult();
                return reopened.Task.WaitAsync(call.Arg<CancellationToken>());
            });

        _worker = _watch.Run("testdb");
        await reading.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await _watch.WhenOpen(_cts.Token).WaitAsync(TimeSpan.FromSeconds(5));
        _nextMove.SetException(new InvalidOperationException("Disconnected before receiving a token"));
        await reopening.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var ready = _watch.WhenOpen(_cts.Token);
        _readyBeforeReopen = ready.IsCompleted;
        _resynchronizedBeforeReopen = _resynchronized.Task.IsCompleted;
        reopened.SetResult(_secondCursor);
        await ready.WaitAsync(TimeSpan.FromSeconds(5));
        await _resynchronized.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact] void should_wait_for_the_reopened_cursor() => _readyBeforeReopen.ShouldBeFalse();
    [Fact] void should_not_resynchronize_before_reopening() => _resynchronizedBeforeReopen.ShouldBeFalse();
    [Fact] void should_reopen_without_a_resume_token() => _options[1].ResumeAfter.ShouldBeNull();
    [Fact] void should_resynchronize_after_reopening() => _resynchronized.Task.IsCompletedSuccessfully.ShouldBeTrue();
    [Fact] void should_release_the_failed_cursor() => _firstCursor.Received(1).Dispose();
}
