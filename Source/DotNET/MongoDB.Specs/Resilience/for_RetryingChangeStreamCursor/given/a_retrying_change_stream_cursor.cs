// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Castle.DynamicProxy;
using Cratis.Arc.MongoDB.Resilience.for_MongoCollectionInterceptorForReturnValue;
using MongoDB.Driver;

namespace Cratis.Arc.MongoDB.Resilience.for_RetryingChangeStreamCursor.given;

public class a_retrying_change_stream_cursor : Specification
{
    protected RetryingChangeStreamCursor<string> _cursor;
    protected IInvocation _invocation;
    protected InvocationTargetWithChangeStreamCursor _target;
    protected IChangeStreamCursor<string> _actualCursor;

    void Establish()
    {
        _actualCursor = Substitute.For<IChangeStreamCursor<string>>();
        _target = new InvocationTargetWithChangeStreamCursor(failCount: 2, successCursor: _actualCursor);

        _invocation = Substitute.For<IInvocation>();
        _invocation.Method.Returns(typeof(InvocationTargetWithChangeStreamCursor).GetMethod(nameof(InvocationTargetWithChangeStreamCursor.WatchAsyncCollectionNotFound)));
        _invocation.InvocationTarget.Returns(_target);
        _invocation.Arguments.Returns([]);

        _cursor = new RetryingChangeStreamCursor<string>(_invocation, TimeSpan.FromMilliseconds(50));
    }
}
