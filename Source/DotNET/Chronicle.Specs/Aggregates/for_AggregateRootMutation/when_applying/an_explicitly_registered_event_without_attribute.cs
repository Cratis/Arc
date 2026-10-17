// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Arc.Chronicle.Aggregates.for_AggregateRootMutation.when_applying;

public class an_explicitly_registered_event_without_attribute : given.an_aggregate_mutation_with_event_registry
{
    RegisteredEvent _event;

    void Establish()
    {
        _event = new();
        _eventTypes.HasFor(typeof(RegisteredEvent)).Returns(true);
    }

    Task Because() => _mutation.Apply(_event);

    [Fact] void should_enroll_the_event_for_append() => _unitOfWork.Received(1).AddEvent(_eventSequenceId, _eventSourceId, _event, Arg.Any<Causation>(), _eventStreamType, _eventStreamId, _eventSourceType, Arg.Any<ConcurrencyScope>());
    [Fact] void should_mutate_the_aggregate() => _mutator.Received(1).Mutate(_event);
    [Fact] void should_retain_the_uncommitted_event() => _mutation.UncommittedEvents.ShouldContainOnly(_event);

    record RegisteredEvent;
}
