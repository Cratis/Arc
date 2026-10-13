// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Reactors.for_CommandSideEffectExecutor.when_handling_refusals;

public class with_validation : given.a_refused_invocation
{
    async Task Because() => _result = await _executor.Execute([_invocation, _later], _context, _store);

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_pass_the_authored_message_to_the_branch() => _received!.Message.ShouldEqual("$strings.refused");
    [Fact] void should_append_the_branch_event() => _appended.Single().Event.ShouldEqual(new RefusalRecorded("$strings.refused"));
    [Fact] void should_route_to_the_observed_source() => _appended.Single().EventSourceId.Value.ShouldEqual("observed");
    [Fact] void should_execute_the_command_not_the_wrapper() => _commandPipeline.Received(1).Execute(_command, _serviceProvider);
    [Fact] void should_skip_later_commands() => _commandPipeline.DidNotReceive().Execute(_later, _serviceProvider);
    [Fact] void should_dispose_the_scope() => _serviceScope.Received(1).Dispose();
}
