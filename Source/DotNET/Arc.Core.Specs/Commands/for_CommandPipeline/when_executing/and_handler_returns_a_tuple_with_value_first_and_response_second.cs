// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Execution;

namespace Cratis.Arc.Commands.for_CommandPipeline.when_executing;

public class and_handler_returns_a_tuple_with_value_first_and_response_second : given.a_command_pipeline_and_a_handler_for_command
{
    CommandResult<string> _result;
    (int, string) _tuple;
    string _errorMessage;

    void Establish()
    {
        _tuple = (42, "Forty two");
        _errorMessage = Guid.NewGuid().ToString();
        _commandHandler.Handle(Arg.Any<CommandContext>()).Returns(_tuple);

        // The int should have a handler, the string should not have a handler (becomes response)
        _commandResponseValueHandlers.CanHandle(Arg.Any<CommandContext>(), _tuple.Item1).Returns(true);
        _commandResponseValueHandlers.CanHandle(Arg.Any<CommandContext>(), _tuple.Item2).Returns(false);
        _commandResponseValueHandlers.Handle(Arg.Any<CommandContext>(), _tuple.Item1).Returns(CommandResult.Error(CorrelationId.New(), _errorMessage));
    }

    async Task Because() => _result = (await _commandPipeline.Execute(_command)) as CommandResult<string>;

    [Fact] void should_call_value_handlers() => _commandResponseValueHandlers.Received(1).Handle(Arg.Any<CommandContext>(), _tuple.Item1);
    [Fact] void should_set_response_on_command_context() => _commandResponseValueHandlers.Received(1).Handle(Arg.Is<CommandContext>(ctx => ctx.Response.Equals(_tuple.Item2)), _tuple.Item1);
    [Fact] void should_return_response_in_command_result() => _result.Response.ShouldEqual(_tuple.Item2);
    [Fact] void should_have_correlation_id() => _result.CorrelationId.ShouldEqual(_correlationId);
    [Fact] void should_return_error_from_value_handlers() => _result.ExceptionMessages.First().ShouldEqual(_errorMessage);
}
