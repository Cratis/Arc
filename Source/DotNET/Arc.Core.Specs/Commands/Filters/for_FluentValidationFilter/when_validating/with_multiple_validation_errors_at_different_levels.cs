// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentValidation;
using FluentValidation.Results;
using ValidationResult = FluentValidation.Results.ValidationResult;

namespace Cratis.Arc.Commands.Filters.for_FluentValidationFilter.when_validating;

public class with_multiple_validation_errors_at_different_levels : given.a_fluent_validation_filter
{
    CommandResult _result;
    IValidator _commandValidator;
    IValidator _nestedValidator;
    ValidationResult _commandValidationResult;
    ValidationResult _nestedValidationResult;
    ComplexCommand _command;

    void Establish()
    {
        var nestedObject = new NestedObject("InvalidNestedValue");
        _command = new ComplexCommand("", nestedObject);
        _context = new CommandContext(_correlationId, typeof(ComplexCommand), _command, [], new());

        _commandValidator = Substitute.For<IValidator>();
        _commandValidationResult = new ValidationResult([
            new ValidationFailure("Name", "Name is required")
        ]);

        _nestedValidator = Substitute.For<IValidator>();
        _nestedValidationResult = new ValidationResult([
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
    [Fact] void should_have_two_validation_results() => _result.ValidationResults.Count().ShouldEqual(2);
    [Fact] void should_have_command_validation_error() => _result.ValidationResults.Any(vr => vr.Message == "Name is required").ShouldBeTrue();
    [Fact] void should_have_nested_validation_error() => _result.ValidationResults.Any(vr => vr.Message == "Nested value is invalid").ShouldBeTrue();
    [Fact] void should_call_command_validator() => _commandValidator.Received(1).ValidateAsync(Arg.Any<IValidationContext>(), Arg.Any<CancellationToken>());
    [Fact] void should_call_nested_validator() => _nestedValidator.Received(1).ValidateAsync(Arg.Any<IValidationContext>(), Arg.Any<CancellationToken>());

    record ComplexCommand(string Name, NestedObject Nested);
    record NestedObject(string Value);
}