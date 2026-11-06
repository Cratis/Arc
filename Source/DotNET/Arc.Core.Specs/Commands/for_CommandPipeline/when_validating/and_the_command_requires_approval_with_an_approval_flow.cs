// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Approvals;

namespace Cratis.Arc.Commands.for_CommandPipeline.when_validating;

public class and_the_command_requires_approval_with_an_approval_flow : given.a_command_pipeline_and_an_approval_requiring_command
{
    CommandResult _result;
    void Establish() => _serviceProvider.GetService(typeof(IEnumerable<IApprovalFlow>)).Returns(new[] { _flow });
    async Task Because() => _result = await _commandPipeline.Validate(_command, _serviceProvider);

    [Fact] void should_validate_normally() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_not_begin_the_flow() => _flow.DidNotReceive().Begin(Arg.Any<CommandContext>());
    [Fact] void should_not_invoke_the_handler() => _commandHandler.DidNotReceive().Handle(Arg.Any<CommandContext>());
}
