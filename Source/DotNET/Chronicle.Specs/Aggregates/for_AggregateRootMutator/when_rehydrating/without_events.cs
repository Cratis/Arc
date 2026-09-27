// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Aggregates.for_AggregateRootMutator.when_rehydrating;

public class without_events : given.an_aggregate_root_mutator
{
    void Establish() => _eventSequence.GetTailSequenceNumber(
        _eventSourceId,
        _aggregateRootContext.EventSourceType,
        _aggregateRootContext.EventStreamType,
        _aggregateRootContext.EventStreamId).Returns(EventSequenceNumber.Unavailable);

    async Task Because() => await _mutator.Rehydrate();

    [Fact] void should_keep_the_initial_tail() => _aggregateRootContext.TailEventSequenceNumber.ShouldEqual(EventSequenceNumber.First);
    [Fact] void should_not_have_events() => _aggregateRootContext.HasEvents.ShouldBeFalse();
}
