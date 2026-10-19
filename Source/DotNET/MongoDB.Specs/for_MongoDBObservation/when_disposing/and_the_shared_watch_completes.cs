// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.MongoDB.for_MongoDBObservation.when_disposing;

public class and_the_shared_watch_completes : given.an_observation
{
    TaskCompletionSource _retryStarted;
    TaskCompletionSource _completed;

    void Establish()
    {
        _retryStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _completed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _snapshot = _ =>
        {
            Interlocked.Increment(ref _attempts);
            return Task.FromException<int>(new Exception("read failed"));
        };
        _delay = (_, token) =>
        {
            var pending = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            _retryStarted.TrySetResult();
            return pending.Task.WaitAsync(token);
        };
    }

    async Task Because()
    {
        Start();
        _subject.Subscribe(_ => { }, () => _completed.TrySetResult());
        await _retryStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        _changes.OnCompleted();
        await _completed.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await _detached.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact] void should_stop_retrying() => _attempts.ShouldEqual(1);
    [Fact] void should_release_the_database_subscription() => _changes.HasObservers.ShouldBeFalse();
}
