// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Transactions;
using Cratis.Execution;

namespace Cratis.Arc.Chronicle.Aggregates.for_AggregateRootMutator.given;

public class an_aggregate_root_mutator : Specification
{
    protected TestAggregateRoot _aggregateRoot;

    protected IEventStore _eventStore;
    protected EventSourceId _eventSourceId;
    protected AggregateRootContext _aggregateRootContext;
    protected IAggregateRootMutator _mutator;
    protected IEventSerializer _eventSerializer;
    protected IAggregateRootEventHandlers _eventHandlers;
    protected IEventSequence _eventSequence;
    protected IUnitOfWork _unitOfWork;
    protected ICorrelationIdAccessor _correlationIdAccessor;

    void Establish()
    {
        _aggregateRoot = new TestAggregateRoot();
        _eventStore = Substitute.For<IEventStore>();
        _eventSourceId = EventSourceId.New();
        _eventSerializer = Substitute.For<IEventSerializer>();
        _eventSequence = Substitute.For<IEventSequence>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _aggregateRootContext = new AggregateRootContext(
            EventSourceType.Default,
            _eventSourceId,
            _aggregateRoot.GetEventStreamType(),
            EventStreamId.Default,
            _eventSequence,
            _aggregateRoot,
            _unitOfWork,
            EventSequenceNumber.First,
            EventSequenceNumber.First);

        _eventSerializer = Substitute.For<IEventSerializer>();
        _eventHandlers = Substitute.For<IAggregateRootEventHandlers>();
        _correlationIdAccessor = Substitute.For<ICorrelationIdAccessor>();

        _mutator = new AggregateRootMutator(
            _aggregateRootContext,
            _eventStore,
            _eventSerializer,
            _eventHandlers,
            _correlationIdAccessor);
    }
}
