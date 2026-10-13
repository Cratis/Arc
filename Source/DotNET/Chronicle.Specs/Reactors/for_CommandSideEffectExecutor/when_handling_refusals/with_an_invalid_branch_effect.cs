// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;

namespace Cratis.Arc.Chronicle.Reactors.for_CommandSideEffectExecutor.when_handling_refusals;

public class with_an_invalid_branch_effect : given.a_refused_invocation
{
    void Establish() => _invocation = Invoke.Command(_command).OnRefused(Refused.Any, _ => new object[] { new RefusalRecorded("valid"), _later });
    async Task Because() => _result = await _executor.Execute([_invocation, _later], _context, _store);

    [Fact] void should_fail() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_not_partially_append() => _eventLog.DidNotReceive().AppendMany(Arg.Any<IEnumerable<EventForEventSourceId>>());
    [Fact] void should_not_execute_the_branch_command() => _commandPipeline.DidNotReceive().Execute(_later, _serviceProvider);
}
