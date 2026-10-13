// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Reactors.SideEffects;

namespace Cratis.Arc.Chronicle.Reactors.for_CommandSideEffectExecutor.when_handling_refusals;

public class without_a_matching_branch : given.a_refused_invocation
{
    ReactorSideEffectFailure _failure;

    void Establish() => _invocation = Invoke.Command(_command).OnRefused(Refused.ByAnyConstraint, Record);
    async Task Because()
    {
        _result = await _executor.Execute([_invocation], _context, _store);
        _result.TryGetError(out _failure);
    }

    [Fact] void should_fail() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_retain_the_existing_failure_message() => _failure.AppendFailures.SelectMany(failure => failure.Errors).ShouldContain("Command 'TestCommand' failed validation: $strings.refused");
    [Fact] void should_not_run_a_branch() => _branchesRun.ShouldEqual(0);
}
