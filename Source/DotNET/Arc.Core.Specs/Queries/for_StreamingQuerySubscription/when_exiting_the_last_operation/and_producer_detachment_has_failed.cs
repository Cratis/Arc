// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Queries.for_StreamingQuerySubscription.when_exiting_the_last_operation;

public class and_producer_detachment_has_failed : Specification
{
    StreamingQuerySubscription _subscription;
    IDisposable _producer;
    IDisposable _resource;
    IServiceScope _scope;
    InvalidOperationException _failure;
    Exception _error;
    bool _resourceDisposedBeforeExit;
    bool _scopeDisposedBeforeExit;
    bool _completedBeforeExit;

    void Establish()
    {
        _subscription = new(CancellationToken.None);
        _producer = Substitute.For<IDisposable>();
        _resource = Substitute.For<IDisposable>();
        _scope = Substitute.For<IServiceScope>();
        _failure = new InvalidOperationException("Producer detachment failed");
        _producer.When(producer => producer.Dispose()).Do(_ => throw _failure);
        _subscription.SetProducer(_producer);
        _subscription.AddResource(_resource);
        _subscription.AddResource(_scope);
        _subscription.TryEnter().ShouldBeTrue();
    }

    void Because()
    {
        _error = Catch.Exception(_subscription.Dispose);
        _subscription.Dispose();
        _resourceDisposedBeforeExit = _resource.ReceivedCalls().Any();
        _scopeDisposedBeforeExit = _scope.ReceivedCalls().Any();
        _completedBeforeExit = _subscription.ResourcesDisposed.IsCompleted;
        _subscription.Exit();
        _subscription.Dispose();
    }

    [Fact] void should_propagate_the_producer_failure() => _error.ShouldEqual(_failure);
    [Fact] void should_attempt_to_detach_the_producer_once() => _producer.Received(1).Dispose();
    [Fact] void should_keep_the_resource_until_the_last_exit() => _resourceDisposedBeforeExit.ShouldBeFalse();
    [Fact] void should_keep_the_scope_until_the_last_exit() => _scopeDisposedBeforeExit.ShouldBeFalse();
    [Fact] void should_not_complete_teardown_before_the_last_exit() => _completedBeforeExit.ShouldBeFalse();
    [Fact] void should_dispose_the_resource_once_after_the_last_exit() => _resource.Received(1).Dispose();
    [Fact] void should_dispose_the_scope_once_after_the_last_exit() => _scope.Received(1).Dispose();
    [Fact] void should_complete_teardown_after_the_last_exit() => _subscription.ResourcesDisposed.IsCompletedSuccessfully.ShouldBeTrue();
}
