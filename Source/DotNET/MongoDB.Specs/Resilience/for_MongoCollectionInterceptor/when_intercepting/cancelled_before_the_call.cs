// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Polly;
using Polly.Retry;

namespace Cratis.Arc.MongoDB.Resilience.for_MongoCollectionInterceptor.when_intercepting;

public class cancelled_before_the_call : given.an_interceptor
{
    protected override string GetInvocationTargetMethod() => nameof(for_MongoCollectionInterceptorForReturnValue.InvocationTarget.SuccessfulMethod);
    bool _completedInTime;

    void Establish()
    {
        // The production pipeline has a retry strategy, and a strategy does not invoke the callback for a token that is
        // already cancelled.
        var pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions { MaxRetryAttempts = 5, Delay = TimeSpan.FromMilliseconds(1) })
            .Build();
        _interceptor = new(pipeline, _semaphore);
        _invocation.Arguments.Returns([new CancellationToken(canceled: true)]);
    }

    async Task Because()
    {
        // A deadline, not a sleep: without the fix the task never completes and this turns a hang into a failure.
        _completedInTime = await Task.WhenAny(Intercept(), Task.Delay(TimeSpan.FromSeconds(10))) == _returnValue;
    }

    Task Intercept()
    {
        _interceptor.Intercept(_invocation);
        return _returnValue;
    }

    [Fact] void should_complete_the_callers_task() => _completedInTime.ShouldBeTrue();
    [Fact] void should_have_cancelled_task() => _returnValue.IsCanceled.ShouldBeTrue();
    [Fact] void should_not_take_a_slot() => _semaphore.CurrentCount.ShouldEqual(PoolSize);
}
