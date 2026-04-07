// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NSubstitute.ExceptionExtensions;

namespace Cratis.Arc.Commands.for_CommandPipeline.when_executing_with_generic_result;

public class and_handler_throws_exception : given.a_command_pipeline_and_a_handler_for_command
{
    CommandResult<string> _result;
    Exception _exception;

    void Establish()
    {
        _exception = new Exception("Something went wrong");
        _commandHandler.Handle(Arg.Any<CommandContext>()).Throws(_exception);
    }

    async Task Because() => _result = await _commandPipeline.Execute<string>(_command, _serviceProvider);

    [Fact] void should_not_be_successful() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_have_exception_message() => _result.ExceptionMessages.First().ShouldEqual(_exception.Message);
    [Fact] void should_have_null_response() => _result.Response.ShouldBeNull();
    [Fact] void should_have_correlation_id() => _result.CorrelationId.ShouldEqual(_correlationId);
    [Fact] void should_set_current_command_context() => _commandContextModifier.Received(1).SetCurrent(Arg.Any<CommandContext>());
}
