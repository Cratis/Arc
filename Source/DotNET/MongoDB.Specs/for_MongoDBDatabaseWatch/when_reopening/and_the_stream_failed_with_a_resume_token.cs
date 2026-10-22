// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.MongoDB.for_MongoDBDatabaseWatch.when_reopening;

public class and_the_stream_failed_with_a_resume_token : given.a_database_watch
{
    async Task Because()
    {
        var reading = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var hold = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        _secondCursor.MoveNextAsync(Arg.Any<CancellationToken>()).Returns(call =>
        {
            reading.TrySetResult();
            return hold.Task.WaitAsync(call.Arg<CancellationToken>());
        });
        _worker = _watch.Run("testdb");
        await _delivered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        _nextMove.SetException(new InvalidOperationException("Disconnected after receiving a token"));
        await reading.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact] void should_resume_after_the_last_delivered_token() => _options[1].ResumeAfter.ShouldEqual(_resumeToken);
    [Fact] void should_not_require_a_new_snapshot() => _resynchronized.Task.IsCompleted.ShouldBeFalse();
    [Fact] void should_release_the_failed_cursor() => _firstCursor.Received(1).Dispose();
}
