// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Execution;

namespace Cratis.Arc.Commands.for_CommandPipeline.when_executing;

public class and_an_execution_scope_participates_while_filters_fail : given.a_command_pipeline_and_a_handler_for_command
{
    CommandResult _result;

    void Establish() => _commandFilters.OnExecution(Arg.Any<CommandContext>()).Returns(CommandResult.Error(CorrelationId.New(), "Not successful"));

    async Task Because() => _result = await _commandPipeline.Execute(_command, _serviceProvider);

    [Fact] void should_begin_the_scope() => _executionScope.Received(1).Begin(Arg.Any<CommandContext>());
    [Fact] void should_complete_the_scope_with_the_failed_result() => _executionScope.Received(1).Complete(Arg.Any<CommandContext>(), _result);
    [Fact] void should_return_not_successful() => _result.IsSuccess.ShouldBeFalse();
}
