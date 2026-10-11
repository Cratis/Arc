// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Reactors.for_CommandSideEffectExecutor.when_executing;

public class with_a_real_system_actor_and_scope_creation_failure : given.a_real_system_executor
{
    void Establish() => _serviceScopeFactory.CreateScope().Returns(_ =>
    {
        _during = _principal.Current!;
        throw new ScopeCreationFailed();
    });

    async Task Because()
    {
        using var callerScope = _principal.BeginScope(_original);
        var execution = _executor.Execute([_command], typeof(InheritedSystemReactor));
        _whilePending = _principal.Current!;
        _error = await Catch.Exception(async () => await execution);
        _after = _principal.Current!;
    }

    [Fact] void should_propagate_the_scope_creation_failure() => _error.ShouldBeOfExactType<ScopeCreationFailed>();
    [Fact] void should_establish_the_inherited_system_role_before_scope_creation() => _during.IsInRole("Administrator").ShouldBeTrue();
    [Fact] void should_keep_the_caller_as_soon_as_setup_fails() => ReferenceEquals(_whilePending, _original).ShouldBeTrue();
    [Fact] void should_keep_the_caller_after_the_setup_failure() => ReferenceEquals(_after, _original).ShouldBeTrue();
    [Fact] void should_not_execute_commands() => _commandPipeline.ReceivedCalls().ShouldBeEmpty();
}
