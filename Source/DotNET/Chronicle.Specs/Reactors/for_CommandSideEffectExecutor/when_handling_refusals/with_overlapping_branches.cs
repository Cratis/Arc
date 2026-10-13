// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Reactors.for_CommandSideEffectExecutor.when_handling_refusals;

public class with_overlapping_branches : given.a_refused_invocation
{
    void Establish() => _invocation = Invoke.Command(_command).OnRefused(Refused.Any, Record).OnRefused(Refused.ByValidation, _ => new RefusalRecorded("wrong"));
    async Task Because() => _result = await _executor.Execute([_invocation], _context, _store);

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_run_only_the_first_matching_branch() => _branchesRun.ShouldEqual(1);
    [Fact] void should_append_only_the_first_branch_event() => _appended.Single().Event.ShouldEqual(new RefusalRecorded("$strings.refused"));
}
