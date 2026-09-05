// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandPipeline.when_executing;

/// <summary>
/// The handler returns a response alongside a value the pipeline hands to a response value handler - the shape of a
/// command that returns a value and appends events. When that handler fails, the command failed.
/// </summary>
public class and_a_response_was_produced_and_a_value_handler_fails : given.a_command_pipeline_and_a_handler_for_command
{
    CommandResult<string> _result;
    (string Response, int Value) _tuple;
    string _errorMessage;

    void Establish()
    {
        _tuple = ("Forty two", 42);
        _errorMessage = Guid.NewGuid().ToString();
        _commandHandler.Handle(Arg.Any<CommandContext>()).Returns(_tuple);
        _commandResponseValueHandlers.CanHandle(Arg.Any<CommandContext>(), _tuple.Response).Returns(false);
        _commandResponseValueHandlers.CanHandle(Arg.Any<CommandContext>(), _tuple.Value).Returns(true);
        _commandResponseValueHandlers.Handle(Arg.Any<CommandContext>(), _tuple.Value).Returns(CommandResult.Error(_correlationId, _errorMessage));
    }

    async Task Because() => _result = (await _commandPipeline.Execute(_command, _serviceProvider)) as CommandResult<string>;

    [Fact] void should_not_be_successful() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_not_carry_the_response() => _result.Response.ShouldBeNull();
    [Fact] void should_keep_the_failure_from_the_value_handler() => _result.ExceptionMessages.ShouldContain(_errorMessage);
    [Fact] void should_still_have_offered_the_response_to_the_value_handler() => _commandResponseValueHandlers.Received(1).Handle(Arg.Is<CommandContext>(ctx => ctx.Response.Equals(_tuple.Response)), _tuple.Value);
}
