// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Reactors.SideEffects;

namespace Cratis.Arc.Chronicle.Reactors.for_CommandSideEffectExecutor.when_handling_refusals;

public class with_an_exception : given.a_refused_invocation
{
    ReactorSideEffectFailure _failure;

    void Establish() => _commandResult.ExceptionMessages = ["unavailable"];
    async Task Because()
    {
        _result = await _executor.Execute([_invocation, _later], _context, _store);
        _result.TryGetError(out _failure);
    }

    [Fact] void should_fail() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_preserve_the_exception_message() => _failure.GetMessages().ShouldContain("Command 'TestCommand' threw an exception: unavailable");
    [Fact] void should_not_run_a_branch() => _branchesRun.ShouldEqual(0);
    [Fact] void should_skip_later_commands() => _commandPipeline.DidNotReceive().Execute(_later, _serviceProvider);
}
