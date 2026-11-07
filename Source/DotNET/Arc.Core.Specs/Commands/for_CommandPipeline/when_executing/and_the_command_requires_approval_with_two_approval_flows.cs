// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Approvals;

namespace Cratis.Arc.Commands.for_CommandPipeline.when_executing;

public class and_the_command_requires_approval_with_two_approval_flows : given.a_command_pipeline_and_an_approval_requiring_command
{
    IApprovalFlow _otherFlow;
    CommandResult _result;
    void Establish()
    {
        _otherFlow = Substitute.For<ApprovalFlowForSpecs>();
        _serviceProvider.GetService(typeof(IEnumerable<IApprovalFlow>)).Returns(new[] { _flow, _otherFlow });
    }
    async Task Because() => _result = await _commandPipeline.Execute(_command, _serviceProvider);

    [Fact] void should_fail_configuration() => _result.AuthorizationConfigurationFailed.ShouldBeTrue();
    [Fact] void should_not_begin_either_flow() => new[] { _flow, _otherFlow }.All(flow => !flow.ReceivedCalls().Any()).ShouldBeTrue();
    [Fact] void should_not_invoke_the_handler() => _commandHandler.DidNotReceive().Handle(Arg.Any<CommandContext>());
}
