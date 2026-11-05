// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Approvals;

namespace Cratis.Arc.Commands.for_CommandPipeline.when_validating;

public class and_the_command_requires_approval_without_an_approval_flow : given.a_command_pipeline_and_an_approval_requiring_command
{
    CommandResult _result;
    async Task Because() => _result = await _commandPipeline.Validate(_command, _serviceProvider);

    [Fact] void should_refuse_authorization() => _result.IsAuthorized.ShouldBeFalse();
    [Fact] void should_explain_the_refusal() => _result.AuthorizationFailureReason.ShouldEqual(ApprovalGate.RefusalReason);
    [Fact] void should_not_invoke_the_handler() => _commandHandler.DidNotReceive().Handle(Arg.Any<CommandContext>());
}
