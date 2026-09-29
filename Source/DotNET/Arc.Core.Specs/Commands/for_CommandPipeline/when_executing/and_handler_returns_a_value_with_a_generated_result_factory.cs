// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandPipeline.when_executing;

public class and_handler_returns_a_value_with_a_generated_result_factory : given.a_command_pipeline_and_a_handler_for_command
{
    CommandResult _result;
    CommandResult _generated;
    GeneratedResponse _value;

    void Establish()
    {
        _value = new("Forty two");
        CommandResultFactories.Register(typeof(GeneratedResponse), (correlationId, response) => _generated = new CommandResult<GeneratedResponse>(correlationId, (GeneratedResponse)response));
        _commandHandler.Handle(Arg.Any<CommandContext>()).Returns(_value);
        _commandResponseValueHandlers.CanHandle(Arg.Any<CommandContext>(), _value).Returns(false);
    }

    async Task Because() => _result = await _commandPipeline.Execute(_command, _serviceProvider);

    [Fact] void should_wrap_the_response_with_the_generated_factory() => _result.ShouldBeSame(_generated);
    [Fact] void should_return_command_result_of_the_response_type() => _result.ShouldBeOfExactType<CommandResult<GeneratedResponse>>();
    [Fact] void should_have_response_value() => ((CommandResult<GeneratedResponse>)_result).Response.ShouldEqual(_value);
    [Fact] void should_have_correlation_id() => _result.CorrelationId.ShouldEqual(_correlationId);
    [Fact] void should_be_successful() => _result.IsSuccess.ShouldBeTrue();

    public record GeneratedResponse(string Value);
}
