// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Approvals;

namespace Cratis.Arc.Commands.for_CommandPipeline.given;

public class a_command_pipeline_and_an_approval_requiring_command : a_command_pipeline
{
    protected object _command;
    protected ICommandHandler _commandHandler;
    internal IApprovalFlow _flow;

    void Establish()
    {
        _command = new ApprovalRequiringCommand();
        _commandHandler = Substitute.For<ICommandHandler>();
        _commandHandler.CommandType.Returns(typeof(ApprovalRequiringCommand));
        _commandHandlerProviders.TryGetHandlerFor(_command, out Arg.Any<ICommandHandler?>()).Returns(call =>
        {
            call[1] = _commandHandler;
            return true;
        });
        _flow = Substitute.For<ApprovalFlowForSpecs>();
    }

    [RequiresApproval]
    internal record ApprovalRequiringCommand;
}
