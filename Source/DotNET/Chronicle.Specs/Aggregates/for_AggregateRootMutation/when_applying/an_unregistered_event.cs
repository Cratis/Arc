// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Aggregates.for_AggregateRootMutation.when_applying;

public class an_unregistered_event : given.an_aggregate_mutation_with_event_registry
{
    Exception _error;

    async Task Because() => _error = await Catch.Exception(() => _mutation.Apply(new UnregisteredEvent()));

    [Fact] void should_reject_the_event_despite_its_attribute() => _error.ShouldBeOfExactType<TypeIsNotAnEventType>();
    [Fact] void should_not_retain_the_event() => _mutation.UncommittedEvents.ShouldBeEmpty();
    [Fact] void should_not_mutate_the_aggregate() => _mutator.DidNotReceive().Mutate(Arg.Any<object>());

    [EventType]
    record UnregisteredEvent;
}
