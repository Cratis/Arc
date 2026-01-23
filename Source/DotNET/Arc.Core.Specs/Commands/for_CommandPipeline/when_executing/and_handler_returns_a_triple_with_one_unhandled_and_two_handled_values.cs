// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Execution;

namespace Cratis.Arc.Commands.for_CommandPipeline.when_executing;

public class and_handler_returns_a_triple_with_one_unhandled_and_two_handled_values : given.a_command_pipeline_and_a_handler_for_command
{
    CommandResult<string> _result;
    (int, string, double) _tuple;

    void Establish()
    {
        _tuple = (42, "Forty two", 3.14);
        _commandHandler.Handle(Arg.Any<CommandContext>()).Returns(_tuple);

        // Only the int and double values have handlers, string becomes the response
        _commandResponseValueHandlers.CanHandle(Arg.Any<CommandContext>(), _tuple.Item1).Returns(true);
        _commandResponseValueHandlers.CanHandle(Arg.Any<CommandContext>(), _tuple.Item2).Returns(false);
        _commandResponseValueHandlers.CanHandle(Arg.Any<CommandContext>(), _tuple.Item3).Returns(true);

        _commandResponseValueHandlers.Handle(Arg.Any<CommandContext>(), _tuple.Item1).Returns(CommandResult.Success(CorrelationId.New()));
        _commandResponseValueHandlers.Handle(Arg.Any<CommandContext>(), _tuple.Item3).Returns(CommandResult.Success(CorrelationId.New()));
    }

    async Task Because() => _result = (await _commandPipeline.Execute(_command, _serviceProvider)) as CommandResult<string>;

    [Fact]
    void should_call_value_handlers_for_handled_values()
    {
        _commandResponseValueHandlers.Received(1).Handle(Arg.Any<CommandContext>(), _tuple.Item1);
        _commandResponseValueHandlers.Received(1).Handle(Arg.Any<CommandContext>(), _tuple.Item3);
    }

    [Fact]
    void should_not_call_value_handlers_for_unhandled_value() =>
        _commandResponseValueHandlers.DidNotReceive().Handle(Arg.Any<CommandContext>(), _tuple.Item2);

    [Fact] void should_set_unhandled_value_as_response() => _result.Response.ShouldEqual(_tuple.Item2);
    [Fact] void should_have_correlation_id() => _result.CorrelationId.ShouldEqual(_correlationId);
    [Fact]
    void should_set_response_on_command_context() =>
        _commandResponseValueHandlers.Received().Handle(Arg.Is<CommandContext>(ctx => ctx.Response!.Equals(_tuple.Item2)), Arg.Any<object>());
}