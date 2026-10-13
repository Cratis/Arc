// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
using Cratis.Execution;

namespace Cratis.Arc.Chronicle.Reactors.for_CommandSideEffectExecutor.when_handling_refusals;

public class with_an_append_failure : given.a_refused_invocation
{
    void Establish() => _eventLog.AppendMany(Arg.Any<IEnumerable<EventForEventSourceId>>()).Returns(AppendManyResult.Failed(CorrelationId.NotSet, [new AppendError("branch append refused")]));
    async Task Because() => _result = await _executor.Execute([_invocation, _later], _context, _store);

    [Fact] void should_fail_instead_of_acknowledging() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_surface_the_append_failure() => (_result.TryGetError(out var failure) && failure.AppendFailures.SelectMany(item => item.Errors).Contains("branch append refused")).ShouldBeTrue();
    [Fact] void should_skip_later_commands() => _commandPipeline.DidNotReceive().Execute(_later, _serviceProvider);
}
