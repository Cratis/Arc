// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Aggregates.for_AggregateRootMutator.when_rehydrating.with_events_in_several_streams;

public class and_the_aggregate_is_loaded_for_a_stream_id_and_source_type : given.events_in_several_streams
{
    protected override EventSourceType EventSourceType => "account";
    protected override EventStreamId EventStreamId => "monthly";

    Task Because() => _mutator.Rehydrate();

    [Fact] void should_apply_every_handled_event_for_the_event_source_id() => _handled.ShouldContainOnly(_inScope, _inAnotherStreamId, _inAnotherSourceType, _withoutRouting, _inAnotherStreamType);
    [Fact] void should_guard_the_last_event_it_applied() => _context.TailEventSequenceNumber.ShouldEqual(_inAnotherStreamType);
}
