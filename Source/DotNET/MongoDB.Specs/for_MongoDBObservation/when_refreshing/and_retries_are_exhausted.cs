// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.MongoDB.for_MongoDBObservation.when_refreshing;

public class and_retries_are_exhausted : given.an_observation
{
    TaskCompletionSource _lastAttempt;

    void Establish()
    {
        _lastAttempt = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _snapshot = _ =>
        {
            if (++_attempts <= 6)
            {
                if (_attempts == 6)
                {
                    _lastAttempt.TrySetResult();
                }
                return Task.FromException<int>(new Exception("read unavailable"));
            }
            return Task.FromResult(_attempts);
        };
    }

    async Task Because()
    {
        Start();
        await _lastAttempt.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Notify();
        await _emitted.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact] void should_bound_the_retry_burst() => _delays.Count.ShouldEqual(5);
    [Fact] void should_back_off_exponentially() => _delays.Select(delay => delay.TotalSeconds).ShouldEqual([1, 2, 4, 8, 16]);
    [Fact] void should_recover_on_a_later_notification() => _values.ShouldContainOnly(7);
    [Fact] void should_keep_the_subscription() => _changes.HasObservers.ShouldBeTrue();
    [Fact] void should_not_report_a_failed_snapshot_as_a_value() => _values.Count.ShouldEqual(1);
}
