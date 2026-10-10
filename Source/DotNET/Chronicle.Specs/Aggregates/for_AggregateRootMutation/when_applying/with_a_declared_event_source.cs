// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.Transactions;
using Cratis.Execution;

namespace Cratis.Arc.Chronicle.Aggregates.for_AggregateRootMutation.when_applying;

public class with_a_declared_event_source : Specification
{
    [EventType]
    record DeclaredSourceEvent;

    IEventSequence _eventSequence;
    IEnumerable<EventForEventSourceId> _appended;
    IReadOnlyDictionary<EventSourceId, ConcurrencyScope> _scopes;
    EventSourceId _eventSourceId;
    EventStreamType _eventStreamType;
    EventStreamId _eventStreamId;
    EventSourceType _eventSourceType;

    async Task Establish()
    {
        _eventSourceId = EventSourceId.New();
        _eventStreamType = "transactions";
        _eventStreamId = "stream";
        _eventSourceType = "account";
        _eventSequence = Substitute.For<IEventSequence>();
        _eventSequence.Id.Returns(EventSequenceId.Log);
        var eventStore = Substitute.For<IEventStore>();
        eventStore.GetEventSequence(Arg.Any<EventSequenceId>()).Returns(_eventSequence);
        _eventSequence
            .AppendMany(Arg.Any<IEnumerable<EventForEventSourceId>>(), Arg.Any<CorrelationId?>(), Arg.Any<IEnumerable<string>?>(), Arg.Any<IDictionary<EventSourceId, ConcurrencyScope>?>())
            .Returns(call =>
            {
                _appended = call.Arg<IEnumerable<EventForEventSourceId>>().ToArray();
                _scopes = new Dictionary<EventSourceId, ConcurrencyScope>(call.Arg<IDictionary<EventSourceId, ConcurrencyScope>>());
                return Task.FromResult(AppendManyResult.Success(CorrelationId.NotSet, []));
            });

        var unitOfWork = new UnitOfWork(CorrelationId.New(), _ => { }, eventStore);
        var context = Substitute.For<IAggregateRootContext, IAggregateRootEventSourceContext>();
        var declared = (IAggregateRootEventSourceContext)context;
        context.EventSourceId.Returns(_eventSourceId);
        context.AggregateRoot.Returns(new TestAggregateRoot());
        context.EventSequence.Returns(_eventSequence);
        context.UnitOfWOrk.Returns(unitOfWork);
        context.EventStreamType.Returns(_eventStreamType);
        context.EventStreamId.Returns(_eventStreamId);
        context.EventSourceType.Returns(_eventSourceType);
        declared.EventSource.Returns(typeof(TestAggregateRoot));
        declared.EventStream.Returns("transactions");

        var eventType = new EventType((EventTypeId)Guid.NewGuid().ToString(), EventTypeGeneration.First, false);
        var handlers = Substitute.For<IAggregateRootEventHandlers>();
        handlers.EventTypes.Returns([eventType]);
        var mutator = new AggregateRootMutator(context, eventStore, Substitute.For<IEventSerializer>(), handlers, Substitute.For<ICorrelationIdAccessor>());
        _eventSequence.GetTailSequenceNumber(_eventSourceId, _eventSourceType, _eventStreamType, _eventStreamId)
            .Returns(EventSequenceNumber.First);
        await mutator.Rehydrate();
        var mutation = new AggregateRootMutation(context, mutator, _eventSequence);
        await mutation.Apply(new DeclaredSourceEvent());
        await unitOfWork.Commit();
    }

    [Fact] void should_append_one_event() => _appended.Count().ShouldEqual(1);
    [Fact] void should_carry_the_event_source() => _appended.Single().EventSource.ShouldEqual(typeof(TestAggregateRoot));
    [Fact] void should_carry_the_declared_stream() => _appended.Single().EventStream.ShouldEqual("transactions");
    [Fact] void should_keep_the_aggregate_stream_type() => _appended.Single().EventStreamType.ShouldEqual(_eventStreamType);
    [Fact] void should_keep_the_aggregate_stream_id() => _appended.Single().EventStreamId.ShouldEqual(_eventStreamId);
    [Fact] void should_keep_the_aggregate_event_source_type() => _appended.Single().EventSourceType.ShouldEqual(_eventSourceType);
    [Fact] void should_guard_the_aggregate_scope() => _scopes[_eventSourceId].EventSourceType.ShouldEqual(_eventSourceType);
    [Fact] void should_guard_the_aggregate_stream_type() => _scopes[_eventSourceId].EventStreamType.ShouldEqual(_eventStreamType);
    [Fact] void should_guard_the_aggregate_stream_id() => _scopes[_eventSourceId].EventStreamId.ShouldEqual(_eventStreamId);
    [Fact] void should_not_narrow_the_guard_to_handled_event_types() => _scopes[_eventSourceId].EventTypes.ShouldBeNull();
}
