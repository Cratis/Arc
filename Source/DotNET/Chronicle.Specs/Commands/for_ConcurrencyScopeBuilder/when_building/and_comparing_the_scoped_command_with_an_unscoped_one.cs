// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Arc.Chronicle.Commands.for_ConcurrencyScopeBuilder.when_building;

/// <summary>
/// The property that failed, stated directly: declaring concurrency must never leave a command with weaker
/// protection than saying nothing. A command that declares nothing yields no scope, so the event sequence
/// applies its configured strategy and the append is checked against a real tail. A command that declares
/// concurrency therefore has to yield a scope the kernel will actually validate — one carrying both an actual
/// sequence number and the event source it applies to. A scope missing either is skipped, which is how the
/// attribute ended up meaning less than its absence.
/// </summary>
public class and_comparing_the_scoped_command_with_an_unscoped_one : given.a_concurrency_scope_builder
{
    ConcurrencyScope? _scoped;
    ConcurrencyScope? _unscoped;

    async Task Because()
    {
        _scoped = await ConcurrencyScopeBuilder.BuildFor(
            CommandContextFor(new CommandScopedForConcurrency()),
            _strategy,
            _eventSourceId);

        _unscoped = await ConcurrencyScopeBuilder.BuildFor(
            CommandContextFor(new CommandWithoutMetadata()),
            _strategy,
            _eventSourceId);
    }

    [Fact] void should_leave_the_unscoped_command_to_the_configured_strategy() => _unscoped.ShouldBeNull();
    [Fact] void should_give_the_scoped_command_a_scope() => _scoped.ShouldNotBeNull();
    [Fact] void should_give_the_scoped_command_something_the_kernel_validates() =>
        (_scoped!.SequenceNumber.IsActualValue && _scoped.EventSourceId is not null).ShouldBeTrue();
}
