// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;
using Polly;

namespace Cratis.Arc.MongoDB.Resilience.for_MongoCollectionInterceptor.given;

public abstract class an_interceptor : Specification
{
    protected const int PoolSize = 10;
    protected ResiliencePipeline _resiliencePipeline;
    protected MongoClientSettings _settings;
    protected MongoCollectionInterceptor _interceptor;
    protected Castle.DynamicProxy.IInvocation _invocation;
    protected Task _returnValue;
    protected for_MongoCollectionInterceptorForReturnValue.InvocationTarget _target;
    protected SemaphoreSlim _semaphore;

    protected abstract string GetInvocationTargetMethod();

    void Establish()
    {
        _resiliencePipeline = new ResiliencePipelineBuilder().Build();

        _semaphore = new SemaphoreSlim(PoolSize, PoolSize);

        _interceptor = new(_resiliencePipeline, _semaphore);

        _invocation = Substitute.For<Castle.DynamicProxy.IInvocation>();
        _invocation.Method.Returns(typeof(for_MongoCollectionInterceptorForReturnValue.InvocationTarget).GetMethod(GetInvocationTargetMethod()));
        _target = new();
        _invocation.InvocationTarget.Returns(_target);
        _invocation.When(_ => _.ReturnValue = Arg.Any<Task>()).Do((_) => _returnValue = _.Arg<Task>());
    }
}
