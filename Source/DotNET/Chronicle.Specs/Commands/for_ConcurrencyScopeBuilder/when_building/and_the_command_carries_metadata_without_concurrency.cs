// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Arc.Chronicle.Commands.for_ConcurrencyScopeBuilder.when_building;

/// <summary>
/// Metadata attributes tag the appended events whether or not they opt into concurrency. Only the opt-in puts a
/// dimension into the scope the builder declares, so metadata alone builds no scope here.
/// </summary>
/// <remarks>
/// That is a statement about the declared scope, and only about it. Returning <see langword="null"/> is not the end
/// of the append's story: it hands off to the strategy configured on the event sequence, which substitutes a scope
/// resolved from the routing metadata the append carries anyway - so a routing-only tag still narrows the check.
/// See <c>for_SingleEventCommandResponseValueHandler.when_handling_with_metadata.without_concurrency_on_event_source_type</c>,
/// which pins the pair this spec cannot see: no scope, and the tag on the append regardless.
/// </remarks>
public class and_the_command_carries_metadata_without_concurrency : given.a_concurrency_scope_builder
{
    ConcurrencyScope? _result;

    async Task Because() => _result = await ConcurrencyScopeBuilder.BuildFor(
        CommandContextFor(new CommandCarryingMetadataWithoutConcurrency()),
        _strategy,
        _eventSourceId);

    [Fact] void should_not_build_a_scope() => _result.ShouldBeNull();
    [Fact] void should_not_resolve_an_expected_sequence_number() =>
        _strategy.DidNotReceive().GetScope(Arg.Any<EventSourceId>(), Arg.Any<EventStreamType?>(), Arg.Any<EventStreamId?>(), Arg.Any<EventSourceType?>(), Arg.Any<IEnumerable<EventType>?>());
}
