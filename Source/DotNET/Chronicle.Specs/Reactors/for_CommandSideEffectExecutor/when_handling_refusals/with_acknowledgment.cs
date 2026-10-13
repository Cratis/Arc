// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;

namespace Cratis.Arc.Chronicle.Reactors.for_CommandSideEffectExecutor.when_handling_refusals;

public class with_acknowledgment : given.a_refused_invocation
{
    void Establish() => _invocation = Invoke.Command(_command).Acknowledge(Refused.Any);
    async Task Because() => _result = await _executor.Execute([_invocation, Invoke.Command(_later)], _context, _store);

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_not_append_events() => _eventLog.DidNotReceive().AppendMany(Arg.Any<IEnumerable<EventForEventSourceId>>());
    [Fact] void should_skip_later_invocations() => _commandPipeline.DidNotReceive().Execute(_later, _serviceProvider);
}
