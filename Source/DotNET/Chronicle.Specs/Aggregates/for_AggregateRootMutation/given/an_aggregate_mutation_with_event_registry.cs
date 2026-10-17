// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Aggregates.for_AggregateRootMutation.given;

public class an_aggregate_mutation_with_event_registry : an_aggregate_mutation
{
    protected IEventTypes _eventTypes;

    void Establish()
    {
        _eventTypes = Substitute.For<IEventTypes>();
        _aggregateRootContext = new AggregateRootContext(
            _eventSourceType,
            _eventSourceId,
            _eventStreamType,
            _eventStreamId,
            _eventSequence,
            _aggregateRoot,
            _unitOfWork,
            EventSequenceNumber.First,
            EventSequenceNumber.BeforeFirst)
        {
            EventTypes = _eventTypes
        };
        _mutation = new AggregateRootMutation(_aggregateRootContext, _mutator, _eventSequence);
    }
}
