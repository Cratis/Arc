// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandPipeline.when_executing_with_generic_result;

public class and_handler_returns_no_value : given.a_command_pipeline_and_a_handler_for_command
{
    CommandResult<string> _result;

    void Establish() => _commandHandler.Handle(Arg.Any<CommandContext>()).Returns(default(object));

    async Task Because() => _result = await _commandPipeline.Execute<string>(_command, _serviceProvider);

    [Fact] void should_be_successful() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_have_null_response() => _result.Response.ShouldBeNull();
    [Fact] void should_have_correlation_id() => _result.CorrelationId.ShouldEqual(_correlationId);
    [Fact] void should_not_have_exceptions() => _result.HasExceptions.ShouldBeFalse();
    [Fact] void should_set_current_command_context() => _commandContextModifier.Received(1).SetCurrent(Arg.Any<CommandContext>());
}
