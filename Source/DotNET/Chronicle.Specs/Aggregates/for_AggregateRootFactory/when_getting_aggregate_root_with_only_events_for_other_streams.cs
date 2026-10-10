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

namespace Cratis.Arc.Chronicle.Aggregates.for_AggregateRootFactory;

/// <summary>
/// An aggregate root without a declared event source whose event source id only has events it does not handle, in
/// other streams: it is still new, while its commit guards those events.
/// </summary>
public class when_getting_aggregate_root_with_only_events_for_other_streams : Specification
{
    [EventType("5d1e5c62-6f7e-4b54-9d0a-3b8c2d6f2a41")]
    record Foreign;

    [EventType("7a2b0e44-1c3d-4e8f-a5b9-6c7d8e9f0a12")]
    record Created;

    EventScenario _scenario;
    EventSourceId _eventSourceId;
    EventSequenceNumber _lastForeign;
    ConcurrencyScope _scope;
    TestAggregateRoot _aggregateRoot;
    AggregateRootFactory _factory;

    async Task Establish()
    {
        _scenario = new EventScenario();
        _eventSourceId = EventSourceId.New();
        await _scenario.EventSequence.Append(_eventSourceId, new Foreign(), "OtherStream");
        _lastForeign = (await _scenario.EventSequence.Append(_eventSourceId, new Foreign())).SequenceNumber;

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

        _factory = new AggregateRootFactory(eventStore, mutatorFactory, unitOfWorkManager, Substitute.For<IServiceProvider>());
    }

    async Task Because()
    {
        _aggregateRoot = await _factory.Get<TestAggregateRoot>(_eventSourceId);
        await _aggregateRoot._mutation.Apply(new Created());
    }

    void Destroy() => _scenario.Dispose();

    [Fact] void should_still_be_new() => _aggregateRoot.IsNewAggregate.ShouldBeTrue();
    [Fact] void should_expect_the_last_event_in_any_stream() => _scope.SequenceNumber.ShouldEqual(_lastForeign);
    [Fact] void should_guard_every_stream_type() => _scope.EventStreamType.ShouldEqual(EventStreamType.All);
}
