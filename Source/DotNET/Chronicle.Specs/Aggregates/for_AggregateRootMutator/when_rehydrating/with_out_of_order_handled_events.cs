// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Aggregates.for_AggregateRootMutator.when_rehydrating;

public class with_out_of_order_handled_events : given.an_aggregate_root_mutator
{
    void Establish()
    {
        _eventHandlers.HasHandleMethods.Returns(true);
        _eventSequence
            .GetFromSequenceNumber(EventSequenceNumber.First, _eventSourceId, Arg.Any<IEnumerable<EventType>>())
            .Returns(ImmutableList<AppendedEvent>.Empty);
        _eventHandlers
            .When(_ => _.Handle(_aggregateRoot, Arg.Any<IEnumerable<EventAndContext>>(), Arg.Any<Action<EventAndContext>>()))
            .Do(call =>
            {
                var handled = call.Arg<Action<EventAndContext>>();
                foreach (var sequence in new[] { (EventSequenceNumber)42UL, EventSequenceNumber.Unavailable, (EventSequenceNumber)12UL })
                {
                    var @event = AppendedEvent.EmptyWithEventSequenceNumber(sequence);
                    handled(new EventAndContext(new object(), @event.Context));
                }
            });
    }

    async Task Because() => await _mutator.Rehydrate();

    [Fact] void should_not_move_the_next_sequence_backwards() => _aggregateRootContext.NextSequenceNumber.ShouldEqual((EventSequenceNumber)43UL);
    [Fact] void should_mark_the_aggregate_as_having_events() => _aggregateRootContext.HasEvents.ShouldBeTrue();
    [Fact] void should_not_replace_the_tail_with_an_unavailable_sequence() => _aggregateRootContext.TailEventSequenceNumber.ShouldEqual(EventSequenceNumber.BeforeFirst);
}
