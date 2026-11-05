// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Approvals;

namespace Cratis.Arc.Commands.for_CommandPipeline.when_executing;

public class and_the_command_requires_approval_and_authorization_denies : given.a_command_pipeline_and_an_approval_requiring_command
{
    CommandResult _result;
    void Establish()
    {
        _serviceProvider.GetService(typeof(IEnumerable<IApprovalFlow>)).Returns(new[] { _flow });
        _commandFilters.OnExecution(Arg.Any<CommandContext>()).Returns(CommandResult.Unauthorized(_correlationId));
    }
    async Task Because() => _result = await _commandPipeline.Execute(_command, _serviceProvider);

    [Fact] void should_refuse_authorization() => _result.IsAuthorized.ShouldBeFalse();
    [Fact] void should_keep_the_original_reason() => (_result.AuthorizationFailureReason == ApprovalGate.RefusalReason).ShouldBeFalse();
    [Fact] void should_not_begin_the_flow() => _flow.DidNotReceive().Begin(Arg.Any<CommandContext>());
    [Fact] void should_not_invoke_the_handler() => _commandHandler.DidNotReceive().Handle(Arg.Any<CommandContext>());
}
