// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Aggregates.for_AggregateRootMutator.when_rehydrating.with_events_in_several_streams;

public class and_the_aggregate_declares_an_event_source : given.events_in_several_streams
{
    protected override EventSourceType EventSourceType => "account";
    protected override EventStreamType EventStreamType => "transactions";
    protected override Type? EventSource => typeof(object);

    Task Because() => _mutator.Rehydrate();

    [Fact] void should_apply_only_the_event_in_its_own_stream() => _handled.ShouldContainOnly(_inScope);
    [Fact] void should_guard_its_stream_type_across_stream_ids() => _context.TailEventSequenceNumber.ShouldEqual(_inAnotherStreamId);
}
