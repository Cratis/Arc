// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
using FluentValidation;
using FluentValidation.Results;

namespace Cratis.Arc.Commands.Filters.for_FluentValidationFilter.when_validating;

public class with_object_containing_null_property : given.a_fluent_validation_filter
{
    CommandResult _result;
    IValidator _validator;
    FluentValidation.Results.ValidationResult _validationResult;
    CommandWithNullProperty _command;

    void Establish()
    {
        _command = new CommandWithNullProperty("ValidName", null);
        _context = new CommandContext(_correlationId, typeof(CommandWithNullProperty), _command, [], new());

        _validator = Substitute.For<IValidator>();
        _validationResult = new FluentValidation.Results.ValidationResult([
            new ValidationFailure("NullProperty", "Property cannot be null")
        ]);

        _discoverableValidators.TryGet(typeof(CommandWithNullProperty), out Arg.Any<IValidator>())
            .Returns(x =>
            {
                x[1] = _validator;
                return true;
            });

        _validator.ValidateAsync(Arg.Any<IValidationContext>(), Arg.Any<CancellationToken>()).Returns(_validationResult);
    }

    async Task Because() => _result = await _filter.OnExecution(_context);

    [Fact] void should_return_unsuccessful_result() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_have_correlation_id() => _result.CorrelationId.ShouldEqual(_correlationId);
    [Fact] void should_be_authorized() => _result.IsAuthorized.ShouldBeTrue();
    [Fact] void should_not_be_valid() => _result.IsValid.ShouldBeFalse();
    [Fact] void should_not_have_exceptions() => _result.HasExceptions.ShouldBeFalse();
    [Fact] void should_have_one_validation_result() => _result.ValidationResults.Count().ShouldEqual(1);
    [Fact] void should_have_validation_result_with_error_severity() => _result.ValidationResults.First().Severity.ShouldEqual(ValidationResultSeverity.Error);
    [Fact] void should_have_validation_result_with_correct_message() => _result.ValidationResults.First().Message.ShouldEqual("Property cannot be null");
    [Fact] void should_have_validation_result_with_correct_member() => _result.ValidationResults.First().Members.ShouldContain("NullProperty");
    [Fact] void should_call_validator() => _validator.Received(1).ValidateAsync(Arg.Any<IValidationContext>(), Arg.Any<CancellationToken>());
    [Fact] void should_not_attempt_to_validate_null_property() => _discoverableValidators.DidNotReceive().TryGet(typeof(NestedObject), out Arg.Any<IValidator>());

    record CommandWithNullProperty(string Name, NestedObject? NullProperty);
    record NestedObject(string Value);
}