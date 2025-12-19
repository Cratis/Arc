// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
using FluentValidation;
using FluentValidation.Results;

namespace Cratis.Arc.Commands.Filters.for_FluentValidationFilter.when_executing_command;

public class with_complex_object_and_nested_validation_fails : given.a_fluent_validation_filter
{
    CommandResult _result;
    IValidator _commandValidator;
    IValidator _nestedValidator;
    FluentValidation.Results.ValidationResult _commandValidationResult;
    FluentValidation.Results.ValidationResult _nestedValidationResult;
    ComplexCommand _command;

    void Establish()
    {
        var nestedObject = new NestedObject("InvalidNestedValue");
        _command = new ComplexCommand("ValidName", nestedObject);
        _context = new CommandContext(_correlationId, typeof(ComplexCommand), _command, [], new());

        _commandValidator = Substitute.For<IValidator>();
        _commandValidationResult = new FluentValidation.Results.ValidationResult();

        _nestedValidator = Substitute.For<IValidator>();
        _nestedValidationResult = new FluentValidation.Results.ValidationResult([
            new ValidationFailure("Value", "Nested value is invalid")
        ]);

        _discoverableValidators.TryGet(typeof(ComplexCommand), out Arg.Any<IValidator>())
            .Returns(x =>
            {
                x[1] = _commandValidator;
                return true;
            });

        _discoverableValidators.TryGet(typeof(NestedObject), out Arg.Any<IValidator>())
            .Returns(x =>
            {
                x[1] = _nestedValidator;
                return true;
            });

        _commandValidator.ValidateAsync(Arg.Any<IValidationContext>(), Arg.Any<CancellationToken>()).Returns(_commandValidationResult);
        _nestedValidator.ValidateAsync(Arg.Any<IValidationContext>(), Arg.Any<CancellationToken>()).Returns(_nestedValidationResult);
    }

    async Task Because() => _result = await _filter.OnExecution(_context);

    [Fact] void should_return_unsuccessful_result() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_have_correlation_id() => _result.CorrelationId.ShouldEqual(_correlationId);
    [Fact] void should_be_authorized() => _result.IsAuthorized.ShouldBeTrue();
    [Fact] void should_not_be_valid() => _result.IsValid.ShouldBeFalse();
    [Fact] void should_not_have_exceptions() => _result.HasExceptions.ShouldBeFalse();
    [Fact] void should_have_one_validation_result() => _result.ValidationResults.Count().ShouldEqual(1);
    [Fact] void should_have_validation_result_with_error_severity() => _result.ValidationResults.First().Severity.ShouldEqual(ValidationResultSeverity.Error);
    [Fact] void should_have_validation_result_with_correct_message() => _result.ValidationResults.First().Message.ShouldEqual("Nested value is invalid");
    [Fact] void should_have_validation_result_with_correct_member() => _result.ValidationResults.First().Members.ShouldContain("Value");
    [Fact] void should_call_command_validator() => _commandValidator.Received(1).ValidateAsync(Arg.Any<IValidationContext>(), Arg.Any<CancellationToken>());
    [Fact] void should_call_nested_validator() => _nestedValidator.Received(1).ValidateAsync(Arg.Any<IValidationContext>(), Arg.Any<CancellationToken>());

    record ComplexCommand(string Name, NestedObject Nested);
    record NestedObject(string Value);
}