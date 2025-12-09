// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
using FluentValidation;
using FluentValidation.Results;
using FluentValidationResult = FluentValidation.Results.ValidationResult;

namespace Cratis.Arc.Commands.Filters.for_FluentValidationFilter.when_executing_command;

public class with_validator_found_and_validation_fails : given.a_fluent_validation_filter
{
    CommandResult _result;
    IValidator _validator;
    FluentValidationResult _validationResult;
    TestCommand _command;

    void Establish()
    {
        _command = new TestCommand("InvalidName");
        _context = new CommandContext(_correlationId, typeof(TestCommand), _command, [], new());

        _validator = Substitute.For<IValidator>();
        _validationResult = new FluentValidationResult([
            new ValidationFailure("Name", "Name is required")
        ]);

        _discoverableValidators.TryGet(typeof(TestCommand), out Arg.Any<IValidator>())
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
    [Fact] void should_have_validation_result_with_correct_message() => _result.ValidationResults.First().Message.ShouldEqual("Name is required");
    [Fact] void should_have_validation_result_with_correct_member() => _result.ValidationResults.First().Members.ShouldContain("Name");
    [Fact] void should_call_validator() => _validator.Received(1).ValidateAsync(Arg.Any<IValidationContext>(), Arg.Any<CancellationToken>());

    record TestCommand(string Name);
}