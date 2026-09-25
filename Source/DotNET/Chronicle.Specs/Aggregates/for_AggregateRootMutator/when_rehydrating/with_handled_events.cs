// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Aggregates.for_AggregateRootMutator.when_rehydrating;

public class with_handled_events : given.an_aggregate_root_mutator
{
    void Establish()
    {
        var @event = AppendedEvent.EmptyWithEventSequenceNumber((EventSequenceNumber)42UL);
        _eventHandlers.HasHandleMethods.Returns(true);
        _eventSequence
            .GetFromSequenceNumber(EventSequenceNumber.First, _eventSourceId, Arg.Any<IEnumerable<EventType>>())
            .Returns(ImmutableList.Create(@event));
        _eventSequence.GetTailSequenceNumber(_eventSourceId, _aggregateRootContext.EventSourceType, _aggregateRootContext.EventStreamType, _aggregateRootContext.EventStreamId).Returns((EventSequenceNumber)42UL);
        _eventHandlers
            .When(_ => _.Handle(_aggregateRoot, Arg.Any<IEnumerable<EventAndContext>>(), Arg.Any<Action<EventAndContext>>()))
            .Do(call => call.Arg<Action<EventAndContext>>()(new EventAndContext(new object(), @event.Context)));
    }

    async Task Because() => await _mutator.Rehydrate();

    [Fact] void should_advance_the_next_sequence_number() => _aggregateRootContext.NextSequenceNumber.ShouldEqual((EventSequenceNumber)43UL);
    [Fact] void should_set_the_tail_event_sequence_number() => _aggregateRootContext.TailEventSequenceNumber.ShouldEqual((EventSequenceNumber)42UL);
}
