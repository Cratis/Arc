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

namespace Cratis.Arc.Chronicle.Aggregates.for_AggregateRootFactory.given;

/// <summary>
/// Two writers load the same, not yet existing, aggregate through the real factory and mutator against an in-process
/// event log, and stage the events they would commit. The concurrency scope each of them carries is captured so the
/// specs can append with it in whichever order they want.
/// </summary>
public class two_writers_creating_the_same_aggregate : Specification
{
    [EventType("0b0c7b40-86a4-4a0b-a5b6-1c6d6e0e8c11")]
    protected record Created;

    protected EventScenario _scenario;
    protected EventSourceId _eventSourceId;
    protected EventStreamType _eventStreamType;
    protected ConcurrencyScope _firstWritersScope;
    protected ConcurrencyScope _secondWritersScope;

    async Task Establish()
    {
        _scenario = new EventScenario();
        _eventSourceId = EventSourceId.New();
        _eventStreamType = new TestAggregateRoot().GetEventStreamType();

        _firstWritersScope = await StageCreation();
        _secondWritersScope = await StageCreation();
    }

    protected Task<AppendResult> Append(ConcurrencyScope scope) =>
        _scenario.EventSequence.Append(
            _eventSourceId,
            new Created(),
            _eventStreamType,
            EventStreamId.Default,
            EventSourceType.Default,
            concurrencyScope: scope);

    void Destroy() => _scenario.Dispose();

    async Task<ConcurrencyScope> StageCreation()
    {
        var captured = ConcurrencyScope.NotSet;
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
            .Do(call => captured = call.Arg<ConcurrencyScope>());

        var eventStore = Substitute.For<IEventStore>();
        eventStore.GetEventSequence(Arg.Any<EventSequenceId>()).Returns(_scenario.EventSequence);

        var handlers = Substitute.For<IAggregateRootEventHandlers>();
        handlers.HasHandleMethods.Returns(false);
        handlers.EventTypes.Returns([]);

        var mutatorFactory = Substitute.For<IAggregateRootMutatorFactory>();
        mutatorFactory
            .Create<TestAggregateRoot>(Arg.Any<AggregateRootContext>())
            .Returns(call => new AggregateRootMutator(
                call.Arg<AggregateRootContext>(),
                eventStore,
                Substitute.For<IEventSerializer>(),
                handlers,
                Substitute.For<ICorrelationIdAccessor>()));

        var unitOfWorkManager = Substitute.For<IUnitOfWorkManager>();
        unitOfWorkManager.HasCurrent.Returns(true);
        unitOfWorkManager.Current.Returns(unitOfWork);

        var factory = new AggregateRootFactory(eventStore, mutatorFactory, unitOfWorkManager, Substitute.For<IServiceProvider>());
        var aggregateRoot = await factory.Get<TestAggregateRoot>(_eventSourceId);

        // The new aggregate has not done anything yet, so the scope is what staging its first event would carry.
        await aggregateRoot._mutation.Apply(new Created());
        return captured;
    }
}
