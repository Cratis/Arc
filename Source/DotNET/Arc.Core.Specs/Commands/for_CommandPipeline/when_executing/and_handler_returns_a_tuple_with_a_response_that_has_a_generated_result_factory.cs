// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandPipeline.when_executing;

public class and_handler_returns_a_tuple_with_a_response_that_has_a_generated_result_factory : given.a_command_pipeline_and_a_handler_for_command
{
    CommandResult _result;
    CommandResult _generated;
    GeneratedResponse _response;
    object _handledValue;

    void Establish()
    {
        _response = new(42);
        _handledValue = new object();
        CommandResultFactories.Register(typeof(GeneratedResponse), (correlationId, response) => _generated = new CommandResult<GeneratedResponse>(correlationId, (GeneratedResponse)response));
        _commandHandler.Handle(Arg.Any<CommandContext>()).Returns((_response, _handledValue));
        _commandResponseValueHandlers.CanHandle(Arg.Any<CommandContext>(), _response).Returns(false);
        _commandResponseValueHandlers.CanHandle(Arg.Any<CommandContext>(), _handledValue).Returns(true);
        _commandResponseValueHandlers.Handle(Arg.Any<CommandContext>(), _handledValue).Returns(CommandResult.Success(_correlationId));
    }

    async Task Because() => _result = await _commandPipeline.Execute(_command, _serviceProvider);

    [Fact] void should_wrap_the_response_with_the_generated_factory() => _result.ShouldBeSame(_generated);
    [Fact] void should_return_command_result_of_the_response_type() => _result.ShouldBeOfExactType<CommandResult<GeneratedResponse>>();
    [Fact] void should_have_response_value() => ((CommandResult<GeneratedResponse>)_result).Response.ShouldEqual(_response);
    [Fact] void should_handle_the_other_value() => _commandResponseValueHandlers.Received(1).Handle(Arg.Any<CommandContext>(), _handledValue);
    [Fact] void should_be_successful() => _result.IsSuccess.ShouldBeTrue();

    public record struct GeneratedResponse(int Value);
}
