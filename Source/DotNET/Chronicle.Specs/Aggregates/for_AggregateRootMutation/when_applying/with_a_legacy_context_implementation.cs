// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.Transactions;
using Cratis.Execution;

namespace Cratis.Arc.Chronicle.Aggregates.for_AggregateRootMutation.when_applying;

/// <summary>
/// An external implementation written against the original <see cref="IAggregateRootContext"/> members only
/// must keep compiling and keep working: it has no declared event source.
/// </summary>
public class with_a_legacy_context_implementation : Specification
{
    [EventType]
    record LegacyEvent;

    IEventSequence _eventSequence;
    IEnumerable<EventForEventSourceId> _appended;
    EventSourceId _eventSourceId;

    async Task Establish()
    {
        _eventSourceId = EventSourceId.New();
        _eventSequence = Substitute.For<IEventSequence>();
        _eventSequence.Id.Returns(EventSequenceId.Log);
        var eventStore = Substitute.For<IEventStore>();
        eventStore.GetEventSequence(Arg.Any<EventSequenceId>()).Returns(_eventSequence);
        _eventSequence
            .AppendMany(Arg.Any<IEnumerable<EventForEventSourceId>>(), Arg.Any<CorrelationId?>(), Arg.Any<IEnumerable<string>?>(), Arg.Any<IDictionary<EventSourceId, ConcurrencyScope>?>())
            .Returns(call =>
            {
                _appended = call.Arg<IEnumerable<EventForEventSourceId>>().ToArray();
                return Task.FromResult(AppendManyResult.Success(CorrelationId.NotSet, []));
            });

        var unitOfWork = new UnitOfWork(CorrelationId.New(), _ => { }, eventStore);
        var context = new LegacyContext(_eventSourceId, _eventSequence, new TestAggregateRoot(), unitOfWork);
        var mutation = new AggregateRootMutation(context, Substitute.For<IAggregateRootMutator>(), _eventSequence);
        await mutation.Apply(new LegacyEvent());
        await unitOfWork.Commit();
    }

    [Fact] void should_append_one_event() => _appended.Count().ShouldEqual(1);
    [Fact] void should_not_record_an_event_source() => _appended.Single().EventSource.ShouldBeNull();
    [Fact] void should_not_record_an_event_stream() => _appended.Single().EventStream.ShouldBeNull();
    [Fact] void should_keep_the_event_source_type() => _appended.Single().EventSourceType.ShouldEqual(new EventSourceType("legacy"));

    sealed class LegacyContext(EventSourceId id, IEventSequence eventSequence, IAggregateRoot aggregateRoot, IUnitOfWork unitOfWork) : IAggregateRootContext
    {
        public EventSourceType EventSourceType { get; } = "legacy";

        public EventSourceId EventSourceId { get; } = id;

        public EventStreamType EventStreamType { get; } = EventStreamType.All;

        public EventStreamId EventStreamId { get; } = EventStreamId.Default;

        public IEventSequence EventSequence { get; } = eventSequence;

        public IAggregateRoot AggregateRoot { get; } = aggregateRoot;

        public IUnitOfWork UnitOfWOrk { get; } = unitOfWork;

        public EventSequenceNumber NextSequenceNumber { get; set; } = EventSequenceNumber.First;

        public EventSequenceNumber TailEventSequenceNumber { get; set; } = EventSequenceNumber.BeforeFirst;

        public bool HasEvents { get; set; }
    }
}
