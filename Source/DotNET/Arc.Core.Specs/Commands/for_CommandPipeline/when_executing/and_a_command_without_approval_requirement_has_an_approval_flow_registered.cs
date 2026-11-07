// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Approvals;

namespace Cratis.Arc.Commands.for_CommandPipeline.when_executing;

public class and_a_command_without_approval_requirement_has_an_approval_flow_registered : given.a_command_pipeline_and_a_handler_for_command
{
    IApprovalFlow _flow;
    void Establish()
    {
        _commandHandler.CommandType.Returns(typeof(string));
        _flow = Substitute.For<ApprovalFlowForSpecs>();
        _serviceProvider.GetService(typeof(IEnumerable<IApprovalFlow>)).Returns(new[] { _flow });
    }
    async Task Because() => await _commandPipeline.Execute(_command, _serviceProvider);

    [Fact] void should_invoke_the_handler_once() => _commandHandler.Received(1).Handle(Arg.Any<CommandContext>());
    [Fact] void should_not_begin_the_flow() => _flow.DidNotReceive().Begin(Arg.Any<CommandContext>());
}
