// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentValidation;

namespace Cratis.Arc.Validation.for_RuleBuilderOptionsExtensions;

public class when_setting_severity_to_warning : Specification
{
    record TestCommand(string Name);

    class TestCommandValidator : BaseValidator<TestCommand>
    {
        public TestCommandValidator()
        {
            RuleFor(x => x.Name).NotEmpty().WithSeverity(ValidationResultSeverity.Warning);
        }
    }

    TestCommandValidator _validator;
    FluentValidation.Results.ValidationResult _result;

    void Establish() => _validator = new TestCommandValidator();

    void Because() => _result = _validator.Validate(new TestCommand(string.Empty));

    [Fact] void should_fail_validation() => _result.IsValid.ShouldBeFalse();
    [Fact] void should_have_warning_severity() => _result.Errors[0].Severity.ShouldEqual(Severity.Warning);
}
