// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Aggregates.for_AggregateRootServiceCollectionExtensions.when_resolving_a_failing_aggregate_root;

public class and_the_factory_task_faults : given.an_aggregate_root_factory_that_fails
{
    protected override void ConfigureFactory(IAggregateRootFactory factory) =>
        factory.Get<SampleAggregateRoot>(Arg.Any<EventSourceId>(), Arg.Any<EventStreamId?>(), Arg.Any<EventSourceType?>())
            .Returns(Task.FromException<SampleAggregateRoot>(_failure));

    void Because() => ResolveAggregateRoot();

    [Fact] void should_throw_the_factory_exception_itself() => _exception.ShouldEqual(_failure);
}
