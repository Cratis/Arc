// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Aggregates.for_AggregateRootServiceCollectionExtensions.when_resolving_a_failing_aggregate_root;

public class and_the_factory_throws_synchronously : given.an_aggregate_root_factory_that_fails
{
    protected override void ConfigureFactory(IAggregateRootFactory factory) =>
        factory.Get<SampleAggregateRoot>(Arg.Any<EventSourceId>(), Arg.Any<EventStreamId?>(), Arg.Any<EventSourceType?>())
            .Returns<Task<SampleAggregateRoot>>(_ => throw _failure);

    void Because() => ResolveAggregateRoot();

    [Fact] void should_throw_a_target_invocation_exception() => _exception.ShouldBeOfExactType<TargetInvocationException>();
    [Fact] void should_carry_the_factory_exception() => _exception.InnerException.ShouldEqual(_failure);
}
