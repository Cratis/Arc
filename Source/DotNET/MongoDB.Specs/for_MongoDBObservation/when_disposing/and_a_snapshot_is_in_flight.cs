// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.MongoDB.for_MongoDBObservation.when_disposing;

public class and_a_snapshot_is_in_flight : given.an_observation
{
    TaskCompletionSource _started;
    TaskCompletionSource<int> _snapshotResult;

    void Establish()
    {
        _started = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _snapshotResult = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _snapshot = _ =>
        {
            _started.TrySetResult();
            return _snapshotResult.Task;
        };
    }

    async Task Because()
    {
        Start();
        await _started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        ((IDisposable)_subject).Dispose();
        _snapshotResult.SetResult(1);
        await _detached.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact] void should_not_publish_the_late_snapshot() => _values.ShouldBeEmpty();
    [Fact] void should_release_the_database_subscription() => _changes.HasObservers.ShouldBeFalse();
}
