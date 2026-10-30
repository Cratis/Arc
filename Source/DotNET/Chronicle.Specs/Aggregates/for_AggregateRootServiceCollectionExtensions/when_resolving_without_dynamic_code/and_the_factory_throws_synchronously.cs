// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Events;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.Aggregates.for_AggregateRootServiceCollectionExtensions.when_resolving_without_dynamic_code;

public class and_the_factory_throws_synchronously : Specification
{
    IAggregateRootFactory _factory;
    InvalidOperationException _failure;
    Exception _exception;

    void Establish()
    {
        _failure = new InvalidOperationException("factory failure");
        _factory = Substitute.For<IAggregateRootFactory>();
        _factory.Get<SampleAggregateRoot>(Arg.Any<EventSourceId>(), null, null)
            .Returns<Task<SampleAggregateRoot>>(_ => throw _failure);
    }

    void Because() => _exception = Catch.Exception(() =>
        AggregateRootServiceCollectionExtensions.GetAggregateRootWithoutDynamicCode(typeof(SampleAggregateRoot), _factory, EventSourceId.New()));

    [Fact] void should_throw_a_target_invocation_exception() => _exception.ShouldBeOfExactType<TargetInvocationException>();
    [Fact] void should_carry_the_factory_exception() => _exception.InnerException.ShouldEqual(_failure);
}
