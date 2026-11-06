// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Approvals;

namespace Cratis.Arc.Commands.for_CommandPipeline.when_executing;

public class and_the_command_requires_approval_with_an_approval_flow : given.a_command_pipeline_and_an_approval_requiring_command
{
    CommandResult _expected;
    CommandResult _result;
    void Establish()
    {
        _expected = CommandResult.Success(_correlationId);
        _flow.Begin(Arg.Any<CommandContext>()).Returns(_expected);
        _serviceProvider.GetService(typeof(IEnumerable<IApprovalFlow>)).Returns(new[] { _flow });
    }
    async Task Because() => _result = await _commandPipeline.Execute(_command, _serviceProvider);

    [Fact] void should_return_the_flow_result() => ReferenceEquals(_result, _expected).ShouldBeTrue();
    [Fact] void should_begin_the_flow_once() => _flow.Received(1).Begin(Arg.Is<CommandContext>(context => context.Command == _command));
    [Fact] void should_not_resolve_arguments() => _commandHandlerArgumentResolver.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_complete_the_execution_scope() => _executionScope.Received(1).Complete(Arg.Any<CommandContext>(), _result);
    [Fact] void should_begin_the_execution_scope() => _executionScope.Received(1).Begin(Arg.Any<CommandContext>());
    [Fact] void should_not_invoke_the_handler() => _commandHandler.DidNotReceive().Handle(Arg.Any<CommandContext>());
}
