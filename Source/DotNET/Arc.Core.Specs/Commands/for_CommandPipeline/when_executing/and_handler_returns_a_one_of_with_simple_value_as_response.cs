// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using OneOf;

namespace Cratis.Arc.Commands.for_CommandPipeline.when_executing;

public class and_handler_returns_a_one_of_with_simple_value_as_response : given.a_command_pipeline_and_a_handler_for_command
{
    CommandResult<string> _result;
    OneOf<string, (int, double)> _oneOf;
    string _value;

    void Establish()
    {
        _value = "Forty two";
        _oneOf = OneOf<string, (int, double)>.FromT0(_value);
        _commandHandler.Handle(Arg.Any<CommandContext>()).Returns(_oneOf);

        _commandResponseValueHandlers.CanHandle(Arg.Any<CommandContext>(), _value).Returns(false);
    }

    async Task Because() => _result = (await _commandPipeline.Execute(_command, _serviceProvider)) as CommandResult<string>;

    [Fact] void should_return_response_in_command_result() => _result.Response.ShouldEqual(_value);
    [Fact] void should_have_correlation_id() => _result.CorrelationId.ShouldEqual(_correlationId);
    [Fact] void should_be_successful() => _result.IsSuccess.ShouldBeTrue();
}
