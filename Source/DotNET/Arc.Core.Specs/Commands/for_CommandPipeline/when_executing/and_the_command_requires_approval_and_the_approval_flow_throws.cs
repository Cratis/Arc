// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Approvals;

namespace Cratis.Arc.Commands.for_CommandPipeline.when_executing;

public class and_the_command_requires_approval_and_the_approval_flow_throws : given.a_command_pipeline_and_an_approval_requiring_command
{
    CommandResult _result;
    void Establish()
    {
        _serviceProvider.GetService(typeof(IEnumerable<IApprovalFlow>)).Returns(new[] { _flow });
        _flow.Begin(Arg.Any<CommandContext>()).Returns<Task<CommandResult>>(_ => throw new InvalidOperationException("Not included in the refusal"));
    }
    async Task Because() => _result = await _commandPipeline.Execute(_command, _serviceProvider);

    [Fact] void should_fail_configuration() => _result.AuthorizationConfigurationFailed.ShouldBeTrue();
    [Fact] void should_not_expose_exception_details() => _result.HasExceptions.ShouldBeFalse();
    [Fact] void should_not_invoke_the_handler() => _commandHandler.DidNotReceive().Handle(Arg.Any<CommandContext>());
}
