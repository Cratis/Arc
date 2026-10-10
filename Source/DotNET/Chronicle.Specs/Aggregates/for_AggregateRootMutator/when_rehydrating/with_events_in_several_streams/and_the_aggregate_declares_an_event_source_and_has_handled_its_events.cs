// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Aggregates.for_AggregateRootMutator.when_rehydrating.with_events_in_several_streams;

public class and_the_aggregate_declares_an_event_source_and_has_handled_its_events : given.events_in_several_streams
{
    protected override EventSourceType EventSourceType => "account";
    protected override EventStreamType EventStreamType => "transactions";
    protected override Type? EventSource => typeof(object);

    void Establish() => _context.NextSequenceNumber = _inScope.Next();

    Task Because() => _mutator.Rehydrate();

    [Fact] void should_not_apply_any_event_again() => _handled.ShouldBeEmpty();
}
