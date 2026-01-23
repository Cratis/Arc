// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Execution;
using OneOf;

namespace Cratis.Arc.Commands.for_CommandPipeline.when_executing;

public class and_handler_returns_a_one_of_with_tuple_containing_response_and_handled_value : given.a_command_pipeline_and_a_handler_for_command
{
    CommandResult<string> _result;
    OneOf<int, (string, double)> _oneOf;
    string _responseValue;
    string _errorMessage;

    void Establish()
    {
        _responseValue = "Forty two";
        _errorMessage = Guid.NewGuid().ToString();
        _oneOf = OneOf<int, (string, double)>.FromT1((_responseValue, 3.14));
        _commandHandler.Handle(Arg.Any<CommandContext>()).Returns(_oneOf);

        _commandResponseValueHandlers.CanHandle(Arg.Any<CommandContext>(), _responseValue).Returns(false);
        _commandResponseValueHandlers.CanHandle(Arg.Any<CommandContext>(), 3.14).Returns(true);
        _commandResponseValueHandlers.Handle(Arg.Any<CommandContext>(), 3.14).Returns(CommandResult.Error(CorrelationId.New(), _errorMessage));
    }

    async Task Because() => _result = (await _commandPipeline.Execute(_command, _serviceProvider)) as CommandResult<string>;

    [Fact] void should_call_value_handlers_for_handled_value() => _commandResponseValueHandlers.Received(1).Handle(Arg.Any<CommandContext>(), 3.14);
    [Fact] void should_set_response_on_command_context() => _commandResponseValueHandlers.Received(1).Handle(Arg.Is<CommandContext>(ctx => ctx.Response.Equals(_responseValue)), 3.14);
    [Fact] void should_return_response_in_command_result() => _result.Response.ShouldEqual(_responseValue);
    [Fact] void should_have_correlation_id() => _result.CorrelationId.ShouldEqual(_correlationId);
    [Fact] void should_return_error_from_value_handlers() => _result.ExceptionMessages.First().ShouldEqual(_errorMessage);
}
