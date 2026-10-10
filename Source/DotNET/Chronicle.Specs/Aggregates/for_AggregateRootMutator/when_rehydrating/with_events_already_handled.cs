// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Aggregates.for_AggregateRootMutator.when_rehydrating;

public class with_events_already_handled : given.an_aggregate_root_mutator
{
    List<EventSequenceNumber> _handled;

    void Establish()
    {
        _aggregateRootContext.NextSequenceNumber = 43UL;
        _handled = [];
        _eventHandlers.HasHandleMethods.Returns(true);
        _eventSequence
            .GetForEventSourceIdAndEventTypes(_eventSourceId, Arg.Any<IEnumerable<EventType>>(), _aggregateRootContext.EventStreamType, _aggregateRootContext.EventStreamId, _aggregateRootContext.EventSourceType)
            .Returns(ImmutableList.Create(
                AppendedEvent.EmptyWithEventSequenceNumber(42UL),
                AppendedEvent.EmptyWithEventSequenceNumber(43UL)));
        _eventSequence.GetTailSequenceNumber(_eventSourceId, _aggregateRootContext.EventSourceType, _aggregateRootContext.EventStreamType, _aggregateRootContext.EventStreamId).Returns((EventSequenceNumber)43UL);
        _eventHandlers
            .When(_ => _.Handle(_aggregateRoot, Arg.Any<IEnumerable<EventAndContext>>(), Arg.Any<Action<EventAndContext>>()))
            .Do(call => _handled.AddRange(call.Arg<IEnumerable<EventAndContext>>().Select(_ => _.Context.SequenceNumber)));
    }

    Task Because() => _mutator.Rehydrate();

    [Fact] void should_only_apply_the_events_after_those_already_handled() => _handled.ShouldContainOnly((EventSequenceNumber)43UL);
}
