// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;

namespace Cratis.Arc.Commands.for_CommandPipeline.when_validating;

public class and_validation_fails : given.a_command_pipeline_and_a_handler_for_command
{
    CommandResult _result;
    ValidationResult _validationError;

    void Establish()
    {
        _validationError = new ValidationResult(ValidationResultSeverity.Error, "Something is wrong", [], null!);
        _commandFilters.OnExecution(Arg.Any<CommandContext>()).Returns(Task.FromResult(new CommandResult
        {
            CorrelationId = _correlationId,
            ValidationResults = [_validationError]
        }));
    }

    async Task Because() => _result = await _commandPipeline.Validate(_command);

    [Fact] void should_not_be_successful() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_have_correlation_id() => _result.CorrelationId.ShouldEqual(_correlationId);
    [Fact] void should_be_authorized() => _result.IsAuthorized.ShouldBeTrue();
    [Fact] void should_not_be_valid() => _result.IsValid.ShouldBeFalse();
    [Fact] void should_have_validation_results() => _result.ValidationResults.ShouldContain(_validationError);
    [Fact] void should_not_have_exceptions() => _result.HasExceptions.ShouldBeFalse();
    [Fact] void should_not_invoke_command_handler() => _commandHandler.DidNotReceive().Handle(Arg.Any<CommandContext>());
}
