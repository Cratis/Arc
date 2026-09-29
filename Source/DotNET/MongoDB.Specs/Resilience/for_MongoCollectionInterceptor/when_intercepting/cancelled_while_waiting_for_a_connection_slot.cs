// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.MongoDB.Resilience.for_MongoCollectionInterceptor.when_intercepting;

public class cancelled_while_waiting_for_a_connection_slot : given.an_interceptor
{
    protected override string GetInvocationTargetMethod() => nameof(for_MongoCollectionInterceptorForReturnValue.InvocationTarget.SuccessfulMethod);
    CancellationTokenSource _cancellation;
    bool _completedInTime;

    void Establish()
    {
        for (var slot = 0; slot < PoolSize; slot++)
        {
            _semaphore.Wait();
        }

        _cancellation = new CancellationTokenSource();
        _invocation.Arguments.Returns([_cancellation.Token]);
    }

    async Task Because()
    {
        _interceptor.Intercept(_invocation);
        await _cancellation.CancelAsync();

        // A deadline, not a sleep: without the fix the task never completes and this turns a hang into a failure.
        _completedInTime = await Task.WhenAny(_returnValue, Task.Delay(TimeSpan.FromSeconds(10))) == _returnValue;
    }

    void Destroy() => _cancellation.Dispose();

    [Fact] void should_complete_the_callers_task() => _completedInTime.ShouldBeTrue();
    [Fact] void should_have_cancelled_task() => _returnValue.IsCanceled.ShouldBeTrue();
    [Fact] void should_not_take_a_slot() => _semaphore.CurrentCount.ShouldEqual(0);
}
