// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Transactions;
using Cratis.Execution;

namespace Cratis.Arc.Chronicle.Aggregates.for_AggregateRootMutator.when_rehydrating;

public class with_a_declared_event_source_and_events_in_other_streams : Specification
{
    IAggregateRootEventHandlers _eventHandlers;
    AggregateRootMutator _mutator;
    AggregateRootContext _context;
    List<EventAndContext> _handled;

    void Establish()
    {
        var aggregateRoot = new TestAggregateRoot();
        var eventSourceId = EventSourceId.New();
        var eventSequence = Substitute.For<IEventSequence>();
        var inScope = EventAt(42UL, "account", "transactions");
        var otherStream = EventAt(43UL, "account", "audit");
        var otherSource = EventAt(44UL, "customer", "transactions");
        eventSequence
            .GetFromSequenceNumber(EventSequenceNumber.First, eventSourceId, Arg.Any<IEnumerable<EventType>>())
            .Returns(ImmutableList.Create(inScope, otherStream, otherSource));
        eventSequence.GetTailSequenceNumber(eventSourceId, Arg.Any<EventSourceType>(), Arg.Any<EventStreamType>(), Arg.Any<EventStreamId>()).Returns((EventSequenceNumber)42UL);

        _context = new AggregateRootContext(
            "account",
            eventSourceId,
            "transactions",
            EventStreamId.Default,
            eventSequence,
            aggregateRoot,
            Substitute.For<IUnitOfWork>(),
            EventSequenceNumber.First,
            EventSequenceNumber.BeforeFirst)
        {
            EventSource = typeof(object),
            EventStream = "transactions"
        };

        _handled = [];
        _eventHandlers = Substitute.For<IAggregateRootEventHandlers>();
        _eventHandlers.HasHandleMethods.Returns(true);
        var serializer = Substitute.For<IEventSerializer>();
        serializer.Deserialize(Arg.Any<AppendedEvent>()).Returns(new object());
        _eventHandlers
            .When(_ => _.Handle(aggregateRoot, Arg.Any<IEnumerable<EventAndContext>>(), Arg.Any<Action<EventAndContext>>()))
            .Do(call => _handled.AddRange(call.Arg<IEnumerable<EventAndContext>>()));
        _mutator = new AggregateRootMutator(_context, Substitute.For<IEventStore>(), serializer, _eventHandlers, Substitute.For<ICorrelationIdAccessor>());
    }

    Task Because() => _mutator.Rehydrate();

    [Fact] void should_only_handle_the_event_in_the_declared_scope() => _handled.Select(_ => _.Context.SequenceNumber).ShouldContainOnly((EventSequenceNumber)42UL);

    static AppendedEvent EventAt(ulong sequenceNumber, string sourceType, string streamType)
    {
        var @event = AppendedEvent.EmptyWithEventSequenceNumber((EventSequenceNumber)sequenceNumber);
        return @event with
        {
            Context = @event.Context with
            {
                EventSourceType = sourceType,
                EventStreamType = streamType,
                EventStreamId = EventStreamId.Default
            }
        };
    }
}
