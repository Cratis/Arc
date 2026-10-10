// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Aggregates.for_AggregateRootMutator.when_rehydrating;

public class and_aggregate_root_does_not_have_any_handle_methods_but_has_events : given.an_aggregate_root_mutator
{
    void Establish()
    {
        _eventSequence.GetTailSequenceNumber(_eventSourceId, _aggregateRootContext.EventSourceType, _aggregateRootContext.EventStreamType, _aggregateRootContext.EventStreamId).Returns((EventSequenceNumber)42L);
    }

    async Task Because() => await _mutator.Rehydrate();

    [Fact] void should_not_handle_any_events() => _eventHandlers.DidNotReceive().Handle(Arg.Any<IAggregateRoot>(), Arg.Any<IEnumerable<EventAndContext>>());
    [Fact] void should_read_the_handled_events_in_the_aggregate_scope() => _eventSequence.Received(1).GetForEventSourceIdAndEventTypes(_eventSourceId, _eventHandlers.EventTypes, _aggregateRootContext.EventStreamType, _aggregateRootContext.EventStreamId, _aggregateRootContext.EventSourceType);
    [Fact] void should_set_the_tail_event_sequence_number() => _aggregateRootContext.TailEventSequenceNumber.ShouldEqual((EventSequenceNumber)42L);
}
