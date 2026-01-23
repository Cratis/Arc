// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Execution;
using OneOf;

namespace Cratis.Arc.Commands.for_CommandPipeline.when_executing;

public class and_handler_returns_a_one_of_with_tuple_containing_all_handled_values : given.a_command_pipeline_and_a_handler_for_command
{
    CommandResult _result;
    OneOf<string, (int, double)> _oneOf;
    string _firstErrorMessage;
    string _secondErrorMessage;

    void Establish()
    {
        _firstErrorMessage = Guid.NewGuid().ToString();
        _secondErrorMessage = Guid.NewGuid().ToString();
        _oneOf = OneOf<string, (int, double)>.FromT1((42, 3.14));
        _commandHandler.Handle(Arg.Any<CommandContext>()).Returns(_oneOf);

        _commandResponseValueHandlers.CanHandle(Arg.Any<CommandContext>(), 42).Returns(true);
        _commandResponseValueHandlers.CanHandle(Arg.Any<CommandContext>(), 3.14).Returns(true);
        _commandResponseValueHandlers.Handle(Arg.Any<CommandContext>(), 42).Returns(CommandResult.Error(CorrelationId.New(), _firstErrorMessage));
        _commandResponseValueHandlers.Handle(Arg.Any<CommandContext>(), 3.14).Returns(CommandResult.Error(CorrelationId.New(), _secondErrorMessage));
    }

    async Task Because() => _result = await _commandPipeline.Execute(_command, _serviceProvider);

    [Fact] void should_call_value_handlers_for_first_value() => _commandResponseValueHandlers.Received(1).Handle(Arg.Any<CommandContext>(), 42);
    [Fact] void should_call_value_handlers_for_second_value() => _commandResponseValueHandlers.Received(1).Handle(Arg.Any<CommandContext>(), 3.14);
    [Fact] void should_return_first_error_from_value_handlers() => _result.ExceptionMessages.ShouldContain(_firstErrorMessage);
    [Fact] void should_return_second_error_from_value_handlers() => _result.ExceptionMessages.ShouldContain(_secondErrorMessage);
    [Fact] void should_have_correlation_id() => _result.CorrelationId.ShouldEqual(_correlationId);
}
