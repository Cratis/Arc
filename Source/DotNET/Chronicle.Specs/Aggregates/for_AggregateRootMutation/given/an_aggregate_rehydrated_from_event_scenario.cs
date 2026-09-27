// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.Testing.EventSequences;
using Cratis.Chronicle.Transactions;
using Cratis.Execution;

namespace Cratis.Arc.Chronicle.Aggregates.for_AggregateRootMutation.given;

public class an_aggregate_rehydrated_from_event_scenario : Specification
{
    [EventType("65120bce-09fb-40be-bd09-120d4fa2985e")]
    protected record Changed;

    protected EventScenario _scenario;
    protected EventSourceId _eventSourceId;
    protected AggregateRootContext _context;
    protected ConcurrencyScope _scope;
    protected AggregateRootMutation _mutation;

    async Task Establish()
    {
        _scenario = new EventScenario();
        _eventSourceId = EventSourceId.New();
        var aggregateRoot = new TestAggregateRoot();
        var streamType = aggregateRoot.GetEventStreamType();
        await _scenario.EventSequence.Append(_eventSourceId, new Changed(), streamType);
        await _scenario.EventSequence.Append(_eventSourceId, new Changed(), streamType);

        var eventType = new EventType((EventTypeId)"65120bce-09fb-40be-bd09-120d4fa2985e", (EventTypeGeneration)1, false);
        var handlers = Substitute.For<IAggregateRootEventHandlers>();
        handlers.HasHandleMethods.Returns(true);
        handlers.EventTypes.Returns([eventType]);
        handlers.When(_ => _.Handle(Arg.Any<IAggregateRoot>(), Arg.Any<IEnumerable<EventAndContext>>(), Arg.Any<Action<EventAndContext>>()))
            .Do(call =>
            {
                foreach (var @event in call.Arg<IEnumerable<EventAndContext>>())
                {
                    call.Arg<Action<EventAndContext>>()(@event);
                }
            });

        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.When(_ => _.AddEvent(
                Arg.Any<EventSequenceId>(),
                _eventSourceId,
                Arg.Any<object>(),
                Arg.Any<Causation>(),
                Arg.Any<EventStreamType>(),
                Arg.Any<EventStreamId>(),
                Arg.Any<EventSourceType>(),
                Arg.Any<ConcurrencyScope>()))
            .Do(call => _scope = call.Arg<ConcurrencyScope>());
        _context = new AggregateRootContext(
            EventSourceType.Default,
            _eventSourceId,
            aggregateRoot.GetEventStreamType(),
            EventStreamId.Default,
            await GetRehydrationEventSequence(),
            aggregateRoot,
            unitOfWork,
            EventSequenceNumber.First,
            EventSequenceNumber.First);
        var serializer = Substitute.For<IEventSerializer>();
        serializer.Deserialize(Arg.Any<AppendedEvent>()).Returns(new Changed());
        var mutator = new AggregateRootMutator(
            _context, Substitute.For<IEventStore>(), serializer, handlers, Substitute.For<ICorrelationIdAccessor>());
        await mutator.Rehydrate();
        _mutation = new AggregateRootMutation(_context, Substitute.For<IAggregateRootMutator>(), _scenario.EventSequence);
    }

    protected virtual Task<IEventSequence> GetRehydrationEventSequence() => Task.FromResult<IEventSequence>(_scenario.EventSequence);

    protected async Task<AppendResult> AppendFromLoadedAggregate()
    {
        await _mutation.Apply(new Changed());
        return await _scenario.EventSequence.Append(
            _eventSourceId,
            new Changed(),
            _context.EventStreamType,
            _context.EventStreamId,
            _context.EventSourceType,
            concurrencyScope: _scope);
    }

    void Destroy() => _scenario.Dispose();
}
