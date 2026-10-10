// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Aggregates.for_AggregateRootMutator.when_rehydrating;

public class with_events_only_in_other_streams : given.an_aggregate_root_mutator
{
    void Establish() =>
        _eventSequence.GetTailSequenceNumber(_eventSourceId, EventSourceType.Default, EventStreamType.All, EventStreamId.Default).Returns((EventSequenceNumber)42UL);

    async Task Because() => await _mutator.Rehydrate();

    [Fact] void should_guard_the_events_in_other_streams() => _aggregateRootContext.TailEventSequenceNumber.ShouldEqual((EventSequenceNumber)42UL);
    [Fact] void should_not_have_events() => _aggregateRootContext.HasEvents.ShouldBeFalse();
}
