// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.MongoDB.for_MongoDBObservation.when_refreshing;

public class and_the_snapshot_fails_once : given.an_observation
{
    void Establish() => _snapshot = _ => ++_attempts == 1 ? Task.FromException<int>(new Exception("read failed")) : Task.FromResult(_attempts);

    async Task Because()
    {
        Start();
        await _emitted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        _emitted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        Notify();
        await _emitted.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact] void should_retry_the_failed_snapshot() => _values.ShouldEqual(new[] { 2, 3 });
    [Fact] void should_back_off_before_retrying() => _delays.ShouldContainOnly(TimeSpan.FromSeconds(1));
    [Fact] void should_keep_the_subscription_alive() => _changes.HasObservers.ShouldBeTrue();
    [Fact] void should_not_terminate_with_an_error() => _error.ShouldBeNull();
}
