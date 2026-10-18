// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.MongoDB.for_MongoDBObservation.when_disposing;

public class and_a_retry_is_pending : given.an_observation
{
    TaskCompletionSource _retryStarted;
    TaskCompletionSource _retryCancelled;

    void Establish()
    {
        _retryStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _retryCancelled = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _snapshot = _ =>
        {
            Interlocked.Increment(ref _attempts);
            return Task.FromException<int>(new Exception("read failed"));
        };
        _delay = async (_, token) =>
        {
            var pending = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            _retryStarted.TrySetResult();
            try
            {
                await pending.Task.WaitAsync(token);
            }
            finally
            {
                _retryCancelled.TrySetResult();
            }
        };
    }

    async Task Because()
    {
        Start();
        await _retryStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        ((IDisposable)_subject).Dispose();
        await _retryCancelled.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await _detached.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact] void should_stop_the_retry() => _attempts.ShouldEqual(1);
    [Fact] void should_detach_from_database_changes() => _changes.HasObservers.ShouldBeFalse();
    [Fact] void should_emit_no_failed_snapshot() => _values.ShouldBeEmpty();
}
