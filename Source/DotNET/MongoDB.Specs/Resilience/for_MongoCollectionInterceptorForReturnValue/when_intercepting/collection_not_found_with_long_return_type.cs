// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.MongoDB.Resilience.for_MongoCollectionInterceptorForReturnValue.when_intercepting;

public class collection_not_found_with_long_return_type : Specification
{
    const int PoolSize = 10;
    MongoCollectionInterceptorForReturnValues _interceptor;
    Castle.DynamicProxy.IInvocation _invocation;
    Task<long> _returnValue;
    InvocationTargetWithCollectionNotFound _target;
    SemaphoreSlim _semaphore;

    void Establish()
    {
        var resiliencePipeline = new Polly.ResiliencePipelineBuilder().Build();
        _semaphore = new SemaphoreSlim(PoolSize, PoolSize);
        _interceptor = new(resiliencePipeline, _semaphore);

        _invocation = Substitute.For<Castle.DynamicProxy.IInvocation>();
        _invocation.Method.Returns(typeof(InvocationTargetWithCollectionNotFound).GetMethod(nameof(InvocationTargetWithCollectionNotFound.CountAsyncCollectionNotFound)));
        _target = new InvocationTargetWithCollectionNotFound();
        _invocation.InvocationTarget.Returns(_target);
        _invocation.When(_ => _.ReturnValue = Arg.Any<Task<long>>()).Do((_) => _returnValue = _.Arg<Task<long>>());
    }

    async Task Because()
    {
        _interceptor.Intercept(_invocation);
        await _returnValue;
    }

    [Fact] void should_not_be_faulted() => _returnValue.IsFaulted.ShouldBeFalse();
    [Fact] async Task should_return_zero() => (await _returnValue).ShouldEqual(0L);
    [Fact] void should_have_freed_up_semaphore() => _semaphore.CurrentCount.ShouldEqual(PoolSize);
}
