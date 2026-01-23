// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Execution;

namespace Cratis.Arc.Commands.for_CommandPipeline.when_executing;

public class and_command_filters_are_reporting_not_successful : given.a_command_pipeline_and_a_handler_for_command
{
    CommandResult _result;

    void Establish()
    {
        _commandFilters.OnExecution(Arg.Any<CommandContext>()).Returns(CommandResult.Error(CorrelationId.New(), "Not successful"));
    }

    async Task Because() => _result = await _commandPipeline.Execute(_command, _serviceProvider);

    [Fact] void should_return_not_successful() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_not_call_command_handler() => _commandHandler.DidNotReceive().Handle(Arg.Any<CommandContext>());
    [Fact] void should_set_current_command_context() => _commandContextModifier.Received(1).SetCurrent(Arg.Any<CommandContext>());
}
