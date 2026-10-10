// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing.EventSequences;
using Cratis.Chronicle.Transactions;
using Cratis.Execution;

namespace Cratis.Arc.Chronicle.Aggregates.for_AggregateRootMutator.given;

/// <summary>
/// Appends events of a handled type for one event source id across streams and source types to a real in-memory
/// kernel, and prepares a mutator to rehydrate an aggregate root over them.
/// </summary>
public class events_in_several_streams : Specification
{
    [EventType("a9f0c6d2-6b0e-4f2f-9d55-2f0c3c7b1e64")]
    protected record Changed;

    protected const string OtherEventStreamType = "OtherStream";
    protected const string OtherEventStreamId = "other-stream-id";
    protected const string OtherEventSourceType = "OtherSource";

    protected EventScenario _scenario;
    protected EventSourceId _eventSourceId;
    protected AggregateRootContext _context;
    protected List<EventSequenceNumber> _handled;
    protected AggregateRootMutator _mutator;

    protected EventSequenceNumber _inScope;
    protected EventSequenceNumber _inAnotherStreamId;
    protected EventSequenceNumber _inAnotherSourceType;
    protected EventSequenceNumber _withoutRouting;
    protected EventSequenceNumber _inAnotherStreamType;

    protected virtual EventSourceType EventSourceType => EventSourceType.Default;
    protected virtual EventStreamType EventStreamType => new TestAggregateRoot().GetEventStreamType();
    protected virtual EventStreamId EventStreamId => EventStreamId.Default;
    protected virtual Type? EventSource => null;

    async Task Establish()
    {
        _scenario = new EventScenario();
        _eventSourceId = EventSourceId.New();

        _inScope = await Append(EventStreamType, EventStreamId, EventSourceType);
        _inAnotherStreamId = await Append(EventStreamType, OtherEventStreamId, EventSourceType);
        _inAnotherSourceType = await Append(EventStreamType, EventStreamId, OtherEventSourceType);

        // Appended last, so the tail an aggregate captures shows whether its scope guards these.
        _withoutRouting = (await _scenario.EventSequence.Append(_eventSourceId, new Changed())).SequenceNumber;
        _inAnotherStreamType = await Append(OtherEventStreamType, EventStreamId, EventSourceType);

        var aggregateRoot = new TestAggregateRoot();
        _context = new AggregateRootContext(
            EventSourceType,
            _eventSourceId,
            EventStreamType,
            EventStreamId,
            _scenario.EventSequence,
            aggregateRoot,
            Substitute.For<IUnitOfWork>(),
            EventSequenceNumber.First,
            EventSequenceNumber.BeforeFirst)
        {
            EventSource = EventSource
        };

        _handled = [];
        var eventType = new EventType((EventTypeId)"a9f0c6d2-6b0e-4f2f-9d55-2f0c3c7b1e64", (EventTypeGeneration)1, false);
        var handlers = Substitute.For<IAggregateRootEventHandlers>();
        handlers.HasHandleMethods.Returns(true);
        handlers.EventTypes.Returns([eventType]);
        handlers.When(_ => _.Handle(Arg.Any<IAggregateRoot>(), Arg.Any<IEnumerable<EventAndContext>>(), Arg.Any<Action<EventAndContext>>()))
            .Do(call =>
            {
                foreach (var @event in call.Arg<IEnumerable<EventAndContext>>())
                {
                    _handled.Add(@event.Context.SequenceNumber);
                    call.Arg<Action<EventAndContext>>()(@event);
                }
            });

        var serializer = Substitute.For<IEventSerializer>();
        serializer.Deserialize(Arg.Any<AppendedEvent>()).Returns(new Changed());

        _mutator = new AggregateRootMutator(_context, Substitute.For<IEventStore>(), serializer, handlers, Substitute.For<ICorrelationIdAccessor>());
    }

    void Destroy() => _scenario.Dispose();

    async Task<EventSequenceNumber> Append(EventStreamType eventStreamType, EventStreamId eventStreamId, EventSourceType eventSourceType) =>
        (await _scenario.EventSequence.Append(_eventSourceId, new Changed(), eventStreamType, eventStreamId, eventSourceType)).SequenceNumber;
}
