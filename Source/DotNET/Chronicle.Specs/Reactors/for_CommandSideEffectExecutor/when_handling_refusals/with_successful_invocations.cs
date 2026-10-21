// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Execution;

namespace Cratis.Arc.Chronicle.Reactors.for_CommandSideEffectExecutor.when_handling_refusals;

public class with_successful_invocations : given.a_refused_invocation
{
    void Establish() => _commandResult = CommandResult.Success(CorrelationId.NotSet);
    async Task Because() => _result = await _executor.Execute([_invocation, _later], _context, _store);

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_not_run_a_refusal_branch() => _branchesRun.ShouldEqual(0);
    [Fact] void should_continue_to_later_commands() => _commandPipeline.Received(1).Execute(_later, _serviceProvider);
}
