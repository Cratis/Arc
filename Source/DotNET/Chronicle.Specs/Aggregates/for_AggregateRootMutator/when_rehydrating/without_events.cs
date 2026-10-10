// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Aggregates.for_AggregateRootMutator.when_rehydrating;

public class without_events : given.an_aggregate_root_mutator
{
    void Establish() => _eventSequence.GetTailSequenceNumber(
        _eventSourceId,
        EventSourceType.Default,
        EventStreamType.All,
        EventStreamId.Default).Returns(EventSequenceNumber.Unavailable);

    async Task Because() => await _mutator.Rehydrate();

    [Fact] void should_keep_expecting_no_event_in_the_scope() => _aggregateRootContext.TailEventSequenceNumber.ShouldEqual(EventSequenceNumber.BeforeFirst);
    [Fact] void should_not_have_events() => _aggregateRootContext.HasEvents.ShouldBeFalse();
}
