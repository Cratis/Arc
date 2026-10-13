// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Reactors.SideEffects;
using Cratis.Execution;

namespace Cratis.Arc.Chronicle.Reactors.for_CommandSideEffectExecutor.when_handling_refusals;

public class with_only_any_for_authorization : given.a_refused_invocation
{
    ReactorSideEffectFailure _failure;

    void Establish()
    {
        _commandResult = CommandResult.Unauthorized(CorrelationId.NotSet, "denied");
        _invocation = Invoke.Command(_command).OnRefused(Refused.Any, Record);
    }
    async Task Because()
    {
        _result = await _executor.Execute([_invocation, _later], _context, _store);
        _result.TryGetError(out _failure);
    }

    [Fact] void should_fail() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_retain_the_existing_failure_message() => _failure.GetMessages().ShouldContain("Command 'TestCommand' was not authorized. denied");
    [Fact] void should_not_run_a_branch() => _branchesRun.ShouldEqual(0);
    [Fact] void should_skip_later_commands() => _commandPipeline.DidNotReceive().Execute(_later, _serviceProvider);
}
