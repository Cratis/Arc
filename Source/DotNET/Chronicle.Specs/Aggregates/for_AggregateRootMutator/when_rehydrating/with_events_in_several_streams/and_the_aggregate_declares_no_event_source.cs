// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Aggregates.for_AggregateRootMutator.when_rehydrating.with_events_in_several_streams;

public class and_the_aggregate_declares_no_event_source : given.events_in_several_streams
{
    Task Because() => _mutator.Rehydrate();

    [Fact] void should_apply_the_event_in_its_own_stream() => _handled.ShouldContain(_inScope);
    [Fact] void should_apply_the_event_in_another_stream_id_its_commit_guards() => _handled.ShouldContain(_inAnotherStreamId);
    [Fact] void should_apply_the_event_for_another_source_type_its_commit_guards() => _handled.ShouldContain(_inAnotherSourceType);
    [Fact] void should_not_apply_the_event_appended_without_routing() => _handled.ShouldNotContain(_withoutRouting);
    [Fact] void should_not_apply_the_event_in_another_stream_type() => _handled.ShouldNotContain(_inAnotherStreamType);
    [Fact] void should_guard_the_last_event_it_applied() => _context.TailEventSequenceNumber.ShouldEqual(_handled.Max());
}
