// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Aggregates.for_AggregateRootMutator.when_rehydrating;

public class with_a_tail_and_events : given.an_aggregate_root_mutator
{
    void Establish()
    {
        _eventSequence.GetFromSequenceNumber(EventSequenceNumber.First, _eventSourceId, Arg.Any<IEnumerable<EventType>>())
            .Returns(ImmutableList<AppendedEvent>.Empty);
        _eventSequence.GetTailSequenceNumber(_eventSourceId, _aggregateRootContext.EventSourceType, _aggregateRootContext.EventStreamType, _aggregateRootContext.EventStreamId)
            .Returns(EventSequenceNumber.First);
    }

    async Task Because() => await _mutator.Rehydrate();

    [Fact] void should_capture_the_tail_before_reading_events() => Received.InOrder(() =>
    {
        _ = _eventSequence.GetTailSequenceNumber(
            _eventSourceId,
            _aggregateRootContext.EventSourceType,
            _aggregateRootContext.EventStreamType,
            _aggregateRootContext.EventStreamId);
        _ = _eventSequence.GetFromSequenceNumber(EventSequenceNumber.First, _eventSourceId, _eventHandlers.EventTypes);
    });
}
