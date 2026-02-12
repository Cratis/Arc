// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandPipeline.when_executing;

public class and_handler_returns_a_triple_with_two_unhandled_values : given.a_command_pipeline_and_a_handler_for_command
{
    CommandResult _result;
    (int, string, double) _tuple;

    void Establish()
    {
        _tuple = (42, "Forty two", 3.14);
        _commandHandler.Handle(Arg.Any<CommandContext>()).Returns(_tuple);

        // Neither int, string, nor double have handlers in this test
        _commandResponseValueHandlers.CanHandle(Arg.Any<CommandContext>(), Arg.Any<object>()).Returns(false);
    }

    async Task Because() => _result = await _commandPipeline.Execute(_command, _serviceProvider);

    [Fact] void should_be_unsuccessful() => _result.IsSuccess.ShouldBeFalse();
}