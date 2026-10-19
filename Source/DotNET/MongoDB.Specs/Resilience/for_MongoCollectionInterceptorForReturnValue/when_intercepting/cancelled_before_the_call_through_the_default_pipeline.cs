// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Polly;

namespace Cratis.Arc.MongoDB.Resilience.for_MongoCollectionInterceptorForReturnValue.when_intercepting;

public class cancelled_before_the_call_through_the_default_pipeline : given.an_interceptor
{
    protected override string GetInvocationTargetMethod() => nameof(InvocationTarget.SuccessfulMethod);
    bool _completedInTime;

    void Establish()
    {
        // MongoDBClientFactory hands the interceptors an empty pipeline, which invokes the callback even for a token that
        // is already cancelled.
        _interceptor = new(ResiliencePipeline.Empty, _semaphore);
        _invocation.Arguments.Returns([new CancellationToken(canceled: true)]);
    }

    async Task Because()
    {
        // A deadline, not a sleep: a task that never completes turns into a failure instead of a hang.
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
