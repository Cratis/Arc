// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;
using FluentValidation;

namespace Cratis.Arc.Validation.for_BaseValidator;

public class when_validating_concept_as_property : Specification
{
    record TestConcept(string Value) : ConceptAs<string>(Value);

    record TestCommand(TestConcept Name, TestConcept Email);

    class TestCommandValidator : BaseValidator<TestCommand>
    {
        public TestCommandValidator()
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required");
            RuleFor(x => x.Email).NotEmpty().EmailAddress().WithMessage("Email is invalid");
        }
    }

    TestCommandValidator _validator;
    FluentValidation.Results.ValidationResult _result;

    void Establish() => _validator = new TestCommandValidator();

    void Because() => _result = _validator.Validate(new TestCommand(new TestConcept(""), new TestConcept("not-an-email")));

    [Fact] void should_fail_validation() => _result.IsValid.ShouldBeFalse();
    [Fact] void should_have_error_for_name_property() => _result.Errors.ShouldContain(error => error.PropertyName == "name");
    [Fact] void should_have_error_for_email_property() => _result.Errors.ShouldContain(error => error.PropertyName == "email");
    [Fact] void should_not_have_error_with_value_in_property_name() => _result.Errors.ShouldNotContain(error => error.PropertyName.Contains("Value"));
}
