// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;

namespace Cratis.Arc.MongoDB.Resilience.for_MongoCollectionInterceptorForReturnValue.when_intercepting;

public class collection_not_found_with_change_stream_cursor_return_type : Specification
{
    const int PoolSize = 10;
    MongoCollectionInterceptorForReturnValues _interceptor;
    Castle.DynamicProxy.IInvocation _invocation;
    Task<IChangeStreamCursor<string>> _returnValue;
    InvocationTargetWithChangeStreamCursor _target;
    SemaphoreSlim _semaphore;

    void Establish()
    {
        var resiliencePipeline = new Polly.ResiliencePipelineBuilder().Build();
        _semaphore = new SemaphoreSlim(PoolSize, PoolSize);
        _interceptor = new(resiliencePipeline, _semaphore);

        _target = new InvocationTargetWithChangeStreamCursor(failCount: 1);
        _invocation = Substitute.For<Castle.DynamicProxy.IInvocation>();
        _invocation.Method.Returns(typeof(InvocationTargetWithChangeStreamCursor).GetMethod(nameof(InvocationTargetWithChangeStreamCursor.WatchAsyncCollectionNotFound)));
        _invocation.InvocationTarget.Returns(_target);
        _invocation.Arguments.Returns([]);
        _invocation.When(_ => _.ReturnValue = Arg.Any<Task<IChangeStreamCursor<string>>>()).Do((_) => _returnValue = _.Arg<Task<IChangeStreamCursor<string>>>());
    }

    async Task Because()
    {
        _interceptor.Intercept(_invocation);
        await _returnValue;
    }

    [Fact] void should_not_be_faulted() => _returnValue.IsFaulted.ShouldBeFalse();
    [Fact] async Task should_return_retrying_cursor() => (await _returnValue).ShouldBeOfExactType<RetryingChangeStreamCursor<string>>();
    [Fact] void should_have_freed_up_semaphore() => _semaphore.CurrentCount.ShouldEqual(PoolSize);
}
