// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Arc.Chronicle.Commands.for_ConcurrencyScopeBuilder.when_building;

/// <summary>
/// Metadata attributes tag the appended events whether or not they opt into concurrency. Only the opt-in puts
/// a dimension into the concurrency scope, so metadata alone must leave the append exactly as it was.
/// </summary>
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
