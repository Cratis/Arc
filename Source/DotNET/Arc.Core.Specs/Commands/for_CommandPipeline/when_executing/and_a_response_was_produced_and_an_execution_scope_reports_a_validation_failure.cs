// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;

namespace Cratis.Arc.Commands.for_CommandPipeline.when_executing;

/// <summary>
/// A commit-time constraint or concurrency violation is a validation failure, not an exception - the shape a
/// transactional scope merges onto the result when the unit of work cannot commit.
/// </summary>
public class and_a_response_was_produced_and_an_execution_scope_reports_a_validation_failure : given.a_command_pipeline_and_a_handler_for_command
{
    CommandResult<string> _result;
    string _response;
    ValidationResult _constraintViolation;

    void Establish()
    {
        _response = "Forty two";
        _constraintViolation = ValidationResult.Error(
            "The value is already claimed by another event source.",
            reason: ValidationResultReason.ConstraintViolation);
        _commandHandler.Handle(Arg.Any<CommandContext>()).Returns(_response);
        _commandResponseValueHandlers.CanHandle(Arg.Any<CommandContext>(), _response).Returns(false);
        _executionScope
            .Complete(Arg.Any<CommandContext>(), Arg.Any<CommandResult>())
            .Returns(callInfo =>
            {
                callInfo.ArgAt<CommandResult>(1).MergeWith(new CommandResult { ValidationResults = [_constraintViolation] });
                return Task.CompletedTask;
            });
    }

    async Task Because() => _result = (await _commandPipeline.Execute(_command, _serviceProvider)) as CommandResult<string>;

    [Fact] void should_not_be_successful() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_not_have_exceptions() => _result.HasExceptions.ShouldBeFalse();
    [Fact] void should_not_carry_the_response() => _result.Response.ShouldBeNull();
    [Fact] void should_keep_the_validation_failure() => _result.ValidationResults.ShouldContain(_constraintViolation);
    [Fact] void should_keep_the_result_authorized() => _result.IsAuthorized.ShouldBeTrue();
    [Fact] void should_keep_the_correlation_id() => _result.CorrelationId.ShouldEqual(_correlationId);
}
