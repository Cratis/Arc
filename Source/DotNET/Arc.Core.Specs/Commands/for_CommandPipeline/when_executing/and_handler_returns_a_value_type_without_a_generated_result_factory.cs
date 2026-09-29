// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandPipeline.when_executing;

public class and_handler_returns_a_value_type_without_a_generated_result_factory : given.a_command_pipeline_and_a_handler_for_command
{
    CommandResult _result;
    UngeneratedResponse _value;

    void Establish()
    {
        _value = new(42);
        _commandHandler.Handle(Arg.Any<CommandContext>()).Returns(_value);
        _commandResponseValueHandlers.CanHandle(Arg.Any<CommandContext>(), _value).Returns(false);
    }

    async Task Because() => _result = await _commandPipeline.Execute(_command, _serviceProvider);

    [Fact] void should_have_no_generated_factory() => CommandResultFactories.For(typeof(UngeneratedResponse)).ShouldBeNull();
    [Fact] void should_return_command_result_of_the_response_type() => _result.ShouldBeOfExactType<CommandResult<UngeneratedResponse>>();
    [Fact] void should_have_response_value() => ((CommandResult<UngeneratedResponse>)_result).Response.ShouldEqual(_value);
    [Fact] void should_have_correlation_id() => _result.CorrelationId.ShouldEqual(_correlationId);
    [Fact] void should_be_successful() => _result.IsSuccess.ShouldBeTrue();

    public record struct UngeneratedResponse(int Value);
}
