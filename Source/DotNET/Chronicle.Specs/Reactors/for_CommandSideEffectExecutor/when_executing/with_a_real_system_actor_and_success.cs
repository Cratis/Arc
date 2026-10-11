// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Execution;

namespace Cratis.Arc.Chronicle.Reactors.for_CommandSideEffectExecutor.when_executing;

public class with_a_real_system_actor_and_success : given.a_real_system_executor
{
    bool _success;

    async Task Because()
    {
        using var callerScope = _principal.BeginScope(_original);
        var execution = _executor.Execute([_command], typeof(InheritedSystemReactor));
        _whilePending = _principal.Current!;
        _commandResult.SetResult(CommandResult.Success(CorrelationId.New()));
        var result = await execution.WaitAsync(TimeSpan.FromSeconds(5));
        _success = result.IsSuccess;
        _after = _principal.Current!;
    }

    [Fact] void should_succeed() => _success.ShouldBeTrue();
    [Fact] void should_execute_with_the_inherited_system_role() => _during.IsInRole("Administrator").ShouldBeTrue();
    [Fact] void should_execute_with_an_authenticated_system_actor() => _during.Identity!.IsAuthenticated.ShouldBeTrue();
    [Fact] void should_keep_the_caller_while_execution_is_pending() => ReferenceEquals(_whilePending, _original).ShouldBeTrue();
    [Fact] void should_keep_the_caller_after_success() => ReferenceEquals(_after, _original).ShouldBeTrue();
}
