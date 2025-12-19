// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NSubstitute.ExceptionExtensions;

namespace Cratis.Arc.Commands.for_CommandPipeline.when_executing;

public class and_an_exception_is_thrown_in_handler : given.a_command_pipeline_and_a_handler_for_command
{
    CommandResult _result;
    Exception _exception;

    void Establish()
    {
        _exception = new Exception("Something went wrong");
        _commandHandler.Handle(Arg.Any<CommandContext>()).Throws(_exception);
    }

    async Task Because() => _result = await _commandPipeline.Execute(_command);

    [Fact] void should_not_call_value_handlers() => _commandResponseValueHandlers.DidNotReceive().Handle(Arg.Any<CommandContext>(), Arg.Any<object>());
    [Fact] void should_be_unsuccessful() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_return_exception_in_command_result() => _result.ExceptionMessages.First().ShouldEqual(_exception.Message);
    [Fact] void should_set_current_command_context() => _commandContextModifier.Received(1).SetCurrent(Arg.Any<CommandContext>());
}
