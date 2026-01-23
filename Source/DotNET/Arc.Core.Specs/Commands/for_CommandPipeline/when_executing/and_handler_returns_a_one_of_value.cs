// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Execution;
using OneOf;

namespace Cratis.Arc.Commands.for_CommandPipeline.when_executing;

public class and_handler_returns_a_one_of_value : given.a_command_pipeline_and_a_handler_for_command
{
    CommandResult _result;
    string _value;
    OneOf<string, int> _oneOf;
    string _errorMessage;

    void Establish()
    {
        _value = "Forty two";
        _errorMessage = Guid.NewGuid().ToString();
        _oneOf = OneOf<string, int>.FromT0(_value);
        _commandHandler.Handle(Arg.Any<CommandContext>()).Returns(_oneOf);
        _commandResponseValueHandlers.CanHandle(Arg.Any<CommandContext>(), _value).Returns(true);
        _commandResponseValueHandlers.Handle(Arg.Any<CommandContext>(), _value).Returns(CommandResult.Error(CorrelationId.New(), _errorMessage));
    }

    async Task Because() => _result = await _commandPipeline.Execute(_command, _serviceProvider);

    [Fact] void should_call_value_handlers() => _commandResponseValueHandlers.Received(1).Handle(Arg.Any<CommandContext>(), _oneOf.Value);
    [Fact] void should_return_error_from_value_handlers() => _result.ExceptionMessages.First().ShouldEqual(_errorMessage);
    [Fact] void should_set_current_command_context() => _commandContextModifier.Received(1).SetCurrent(Arg.Any<CommandContext>());
}
