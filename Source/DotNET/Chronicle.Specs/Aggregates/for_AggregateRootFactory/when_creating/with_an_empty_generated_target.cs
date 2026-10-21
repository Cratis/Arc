// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Aggregates.for_AggregateRootFactory.when_creating;

public class with_an_empty_generated_target : given.a_generated_aggregate_target
{
    async Task Because() => _aggregate = await InInvocation(() => _factory.Get<NewAggregate>(_id));

    [Fact] void should_activate_the_aggregate() => _aggregate.Activated.ShouldBeTrue();
    [Fact] void should_check_whole_source_emptiness() => _sequence.Received(1).HasEventsFor(_id);
    [Fact] void should_rehydrate_once() => _mutator.Received(1).Rehydrate();
}
