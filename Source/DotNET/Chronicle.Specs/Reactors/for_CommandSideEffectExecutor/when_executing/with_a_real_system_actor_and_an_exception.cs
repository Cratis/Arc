// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Reactors.for_CommandSideEffectExecutor.when_executing;

public class with_a_real_system_actor_and_an_exception : given.a_real_system_executor
{
    async Task Because()
    {
        using var callerScope = _principal.BeginScope(_original);
        var execution = _executor.Execute([_command], typeof(InheritedSystemReactor));
        _whilePending = _principal.Current!;
        _commandResult.SetException(new CommandExecutionFailed());
        _error = await Catch.Exception(async () => await execution.WaitAsync(TimeSpan.FromSeconds(5)));
        _after = _principal.Current!;
    }

    [Fact] void should_propagate_the_command_exception() => _error.ShouldBeOfExactType<CommandExecutionFailed>();
    [Fact] void should_execute_with_the_inherited_system_role() => _during.IsInRole("Administrator").ShouldBeTrue();
    [Fact] void should_keep_the_caller_while_execution_is_pending() => ReferenceEquals(_whilePending, _original).ShouldBeTrue();
    [Fact] void should_keep_the_caller_after_the_exception() => ReferenceEquals(_after, _original).ShouldBeTrue();
}
