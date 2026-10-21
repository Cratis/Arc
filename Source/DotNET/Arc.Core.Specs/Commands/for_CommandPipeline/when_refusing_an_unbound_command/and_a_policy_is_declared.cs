// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandPipeline.when_refusing_an_unbound_command;

public class and_a_policy_is_declared : given.an_unbound_command
{
    async Task Because() => _result = await _commandPipeline.RefuseUnboundHosted(new UnboundCommandRefusalRequest(typeof(PolicyCommand), _bindingFailure, _serviceProvider, false), CancellationToken.None);

    [Fact] void should_report_validation_for_an_authenticated_caller() => _result.IsValid.ShouldBeFalse();
    [Fact] void should_not_evaluate_a_resource_policy() => _policy.Calls.ShouldEqual(0);
    [Fact] void should_not_call_resource_filters() => _commandFilters.DidNotReceive().OnExecution(Arg.Any<CommandContext>());
}
