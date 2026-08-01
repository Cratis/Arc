// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Arc.Chronicle.Commands.for_ConcurrencyScopeBuilder.when_building;

/// <summary>
/// The scope has to carry an expected sequence number, resolved for the stream being appended to. Without one
/// the kernel skips validation, so a command that declares concurrency appends unchecked — and because the
/// scope is not NotSet it also displaces the strategy the event sequence would otherwise apply, leaving the
/// command with less protection than if it had said nothing at all.
/// </summary>
public class and_the_command_is_scoped_for_concurrency : given.a_concurrency_scope_builder
{
    ConcurrencyScope? _result;

    async Task Because() => _result = await ConcurrencyScopeBuilder.BuildFor(
        CommandContextFor(new CommandScopedForConcurrency()),
        _strategy,
        _eventSourceId);

    [Fact] void should_build_a_scope() => _result.ShouldNotBeNull();
    [Fact] void should_carry_an_expected_sequence_number() => _result!.SequenceNumber.IsActualValue.ShouldBeTrue();
    [Fact] void should_scope_to_the_event_source_being_appended_to() => _result!.EventSourceId.ShouldEqual(_eventSourceId);
    [Fact] void should_resolve_the_expected_sequence_number_for_that_event_source() =>
        _strategy.Received(1).GetScope(_eventSourceId, Arg.Any<EventStreamType?>(), Arg.Any<EventStreamId?>(), Arg.Any<EventSourceType?>(), Arg.Any<IEnumerable<EventType>?>());
}
