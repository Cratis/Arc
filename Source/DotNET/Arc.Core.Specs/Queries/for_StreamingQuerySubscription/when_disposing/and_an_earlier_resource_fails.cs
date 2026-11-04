// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_StreamingQuerySubscription.when_disposing;

public class and_an_earlier_resource_fails : Specification
{
    StreamingQuerySubscription _subscription;
    IDisposable _second;
    Exception _error;
    Exception _failure;
    List<Exception> _reported;

    void Establish()
    {
        _reported = [];
        _subscription = new(CancellationToken.None, _reported.Add);
        _failure = new InvalidOperationException("cleanup failed");
        var first = Substitute.For<IDisposable>();
        first.When(_ => _.Dispose()).Do(_ => throw _failure);
        _second = Substitute.For<IDisposable>();
        _subscription.AddResource(first);
        _subscription.AddResource(_second);
    }

    void Because() => _error = Catch.Exception(_subscription.Dispose);

    [Fact] void should_not_throw() => _error.ShouldBeNull();
    [Fact] void should_dispose_the_second_resource_once() => _second.Received(1).Dispose();
    [Fact] void should_report_the_failure_once() => _reported.ShouldEqual([_failure]);
}
