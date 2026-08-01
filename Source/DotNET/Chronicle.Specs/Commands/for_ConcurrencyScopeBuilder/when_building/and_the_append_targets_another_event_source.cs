// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Commands.for_ConcurrencyScopeBuilder.when_building;

/// <summary>
/// A command can append across streams, and an expected tail belongs to exactly one of them. The scope is
/// therefore built for the event source actually being written to rather than for the command's own — one
/// scope reused across targets would hold one stream's expected tail and be wrong for every other.
/// </summary>
public class and_the_append_targets_another_event_source : given.a_concurrency_scope_builder
{
    readonly EventSourceId _otherEventSourceId = EventSourceId.New();

    async Task Because() => await ConcurrencyScopeBuilder.BuildFor(
        CommandContextFor(new CommandScopedForConcurrency()),
        _strategy,
        _otherEventSourceId);

    [Fact] void should_resolve_the_expected_sequence_number_for_the_target_event_source() =>
        _strategy.Received(1).GetScope(_otherEventSourceId, Arg.Any<EventStreamType?>(), Arg.Any<EventStreamId?>(), Arg.Any<EventSourceType?>(), Arg.Any<IEnumerable<EventType>?>());
}
