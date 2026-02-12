// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Execution;

namespace Cratis.Arc.Commands.for_CommandPipeline.when_executing;

public class and_handler_returns_a_triple_with_all_handled_values : given.a_command_pipeline_and_a_handler_for_command
{
    CommandResult _result;
    (int, string, double) _tuple;

    void Establish()
    {
        _tuple = (42, "Forty two", 3.14);
        _commandHandler.Handle(Arg.Any<CommandContext>()).Returns(_tuple);

        // All values have handlers
        _commandResponseValueHandlers.CanHandle(Arg.Any<CommandContext>(), _tuple.Item1).Returns(true);
        _commandResponseValueHandlers.CanHandle(Arg.Any<CommandContext>(), _tuple.Item2).Returns(true);
        _commandResponseValueHandlers.CanHandle(Arg.Any<CommandContext>(), _tuple.Item3).Returns(true);

        _commandResponseValueHandlers.Handle(Arg.Any<CommandContext>(), _tuple.Item1).Returns(CommandResult.Success(CorrelationId.New()));
        _commandResponseValueHandlers.Handle(Arg.Any<CommandContext>(), _tuple.Item2).Returns(CommandResult.Success(CorrelationId.New()));
        _commandResponseValueHandlers.Handle(Arg.Any<CommandContext>(), _tuple.Item3).Returns(CommandResult.Success(CorrelationId.New()));
    }

    async Task Because() => _result = await _commandPipeline.Execute(_command, _serviceProvider);

    [Fact]
    void should_call_value_handlers_for_all_values()
    {
        _commandResponseValueHandlers.Received(1).Handle(Arg.Any<CommandContext>(), _tuple.Item1);
        _commandResponseValueHandlers.Received(1).Handle(Arg.Any<CommandContext>(), _tuple.Item2);
        _commandResponseValueHandlers.Received(1).Handle(Arg.Any<CommandContext>(), _tuple.Item3);
    }

    [Fact] void should_not_set_response() => _result.ShouldBeOfExactType<CommandResult>();
    [Fact] void should_be_successful() => _result.IsSuccess.ShouldBeTrue();
}