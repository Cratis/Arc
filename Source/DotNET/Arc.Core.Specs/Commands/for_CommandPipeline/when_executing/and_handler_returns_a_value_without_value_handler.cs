// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandPipeline.when_executing;

public class and_handler_returns_a_value_without_value_handler : given.a_command_pipeline_and_a_handler_for_command
{
    CommandResult _result;
    string _value;

    void Establish()
    {
        _value = "Forty two";
        _commandHandler.Handle(Arg.Any<CommandContext>()).Returns(_value);
        _commandResponseValueHandlers.CanHandle(Arg.Any<CommandContext>(), _value).Returns(false);
    }

    async Task Because() => _result = await _commandPipeline.Execute(_command);

    [Fact] void should_check_if_value_handlers_can_handle() => _commandResponseValueHandlers.Received(1).CanHandle(Arg.Any<CommandContext>(), _value);
    [Fact] void should_not_call_value_handlers() => _commandResponseValueHandlers.DidNotReceive().Handle(Arg.Any<CommandContext>(), _value);
    [Fact] void should_return_command_result_with_response() => _result.ShouldBeOfExactType<CommandResult<string>>();
    [Fact] void should_have_response_value() => ((CommandResult<string>)_result).Response.ShouldEqual(_value);
    [Fact] void should_have_correlation_id() => _result.CorrelationId.ShouldEqual(_correlationId);
    [Fact] void should_be_successful() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_set_current_command_context() => _commandContextModifier.Received(1).SetCurrent(Arg.Any<CommandContext>());
}