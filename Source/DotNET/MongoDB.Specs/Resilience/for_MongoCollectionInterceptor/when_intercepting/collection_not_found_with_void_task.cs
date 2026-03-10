// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.MongoDB.Resilience.for_MongoCollectionInterceptor.when_intercepting;

public class collection_not_found_with_void_task : Specification
{
    const int PoolSize = 10;
    MongoCollectionInterceptor _interceptor;
    Castle.DynamicProxy.IInvocation _invocation;
    Task _returnValue;
    InvocationTargetWithCollectionNotFound _target;
    SemaphoreSlim _semaphore;

    void Establish()
    {
        var resiliencePipeline = new Polly.ResiliencePipelineBuilder().Build();
        _semaphore = new SemaphoreSlim(PoolSize, PoolSize);
        _interceptor = new(resiliencePipeline, _semaphore);

        _invocation = Substitute.For<Castle.DynamicProxy.IInvocation>();
        _invocation.Method.Returns(typeof(InvocationTargetWithCollectionNotFound).GetMethod(nameof(InvocationTargetWithCollectionNotFound.DeleteAsyncCollectionNotFound)));
        _target = new InvocationTargetWithCollectionNotFound();
        _invocation.InvocationTarget.Returns(_target);
        _invocation.When(_ => _.ReturnValue = Arg.Any<Task>()).Do((_) => _returnValue = _.Arg<Task>());
    }

    async Task Because()
    {
        _interceptor.Intercept(_invocation);
        await _returnValue;
    }

    [Fact] void should_not_be_faulted() => _returnValue.IsFaulted.ShouldBeFalse();
    [Fact] void should_complete_successfully() => _returnValue.IsCompletedSuccessfully.ShouldBeTrue();
    [Fact] void should_have_freed_up_semaphore() => _semaphore.CurrentCount.ShouldEqual(PoolSize);
}
