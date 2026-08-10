// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;

namespace Cratis.Arc.Commands.for_CommandPipeline.when_executing;

/// <summary>
/// Removing the response is gated on the command not succeeding - never on validation results merely being present.
/// A warning at or below the allowed severity does not block the command, so the response it produced is its own.
/// </summary>
public class and_a_response_was_produced_and_the_command_succeeded_with_warnings : given.a_command_pipeline_and_a_handler_for_command
{
    CommandResult<string> _result;
    string _response;
    ValidationResult _warning;

    void Establish()
    {
        _response = "Forty two";
        _warning = ValidationResult.Warning("The value is unusually large.");
        _commandFilters
            .OnExecution(Arg.Any<CommandContext>())
            .Returns(new CommandResult { CorrelationId = _correlationId, ValidationResults = [_warning] });
        _commandHandler.Handle(Arg.Any<CommandContext>()).Returns(_response);
        _commandResponseValueHandlers.CanHandle(Arg.Any<CommandContext>(), _response).Returns(false);
    }

    async Task Because() => _result = (await _commandPipeline.Execute(_command, _serviceProvider, ValidationResultSeverity.Warning)) as CommandResult<string>;

    [Fact] void should_be_successful() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_carry_the_response() => _result.Response.ShouldEqual(_response);
    [Fact] void should_have_run_the_handler() => _commandHandler.Received(1).Handle(Arg.Any<CommandContext>());
    [Fact] void should_complete_the_scope() => _executionScope.Received(1).Complete(Arg.Any<CommandContext>(), Arg.Any<CommandResult>());
}
