// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Queries.for_StreamingQuerySubscription.when_disposing;

public class and_cancellation_and_producer_detachment_fail : Specification
{
    StreamingQuerySubscription _subscription;
    IDisposable _producer;
    IDisposable _resource;
    IServiceScope _scope;
    CancellationTokenRegistration _registration;
    InvalidOperationException _callbackFailure;
    InvalidOperationException _producerFailure;
    Exception _error;
    bool _completedBeforeTheExceptionReturned;

    void Establish()
    {
        _subscription = new(CancellationToken.None);
        _producer = Substitute.For<IDisposable>();
        _resource = Substitute.For<IDisposable>();
        _scope = Substitute.For<IServiceScope>();
        _callbackFailure = new InvalidOperationException("Cancellation failed");
        _producerFailure = new InvalidOperationException("Producer detachment failed");
        _registration = _subscription.Token.Register(() => throw _callbackFailure);
        _producer.When(producer => producer.Dispose()).Do(_ => throw _producerFailure);
        _subscription.SetProducer(_producer);
        _subscription.AddResource(_resource);
        _subscription.AddResource(_scope);
    }

    void Because()
    {
        _error = Catch.Exception(_subscription.Dispose);
        _completedBeforeTheExceptionReturned = _subscription.ResourcesDisposed.IsCompletedSuccessfully;
        _subscription.Dispose();
    }

    [Fact] void should_aggregate_both_failures() => _error.ShouldBeOfExactType<AggregateException>();
    [Fact] void should_report_exactly_two_failures() => ((AggregateException)_error).InnerExceptions.Count.ShouldEqual(2);
    [Fact] void should_report_the_cancellation_failure_first() => ((AggregateException)((AggregateException)_error).InnerExceptions[0]).InnerExceptions.Single().ShouldEqual(_callbackFailure);
    [Fact] void should_report_the_producer_failure_second() => ((AggregateException)_error).InnerExceptions[1].ShouldEqual(_producerFailure);
    [Fact] void should_complete_teardown_before_propagating_the_failures() => _completedBeforeTheExceptionReturned.ShouldBeTrue();
    [Fact] void should_attempt_to_detach_the_producer_once() => _producer.Received(1).Dispose();
    [Fact] void should_dispose_the_resource_once() => _resource.Received(1).Dispose();
    [Fact] void should_dispose_the_scope_once() => _scope.Received(1).Dispose();

    void Destroy() => _registration.Dispose();
}
