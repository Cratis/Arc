// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
using Cratis.Monads;
using Catch = Cratis.Specifications.Catch;

namespace Cratis.Arc.Testing.Commands.for_CommandRejectionShouldExtensions.when_asserting_a_validation_reason;

public class and_another_reason_was_given : Specification
{
    Exception _error;
    void Because()
    {
        Result<string, ValidationResult> result = ValidationResult.Error("Unavailable", reason: ValidationResultReason.ValidatorFailed);
        _error = Catch.Exception(() => result.ShouldHaveValidationErrorBecauseOf(ValidationResultReason.Rule));
    }
    [Fact] void should_fail() => _error.ShouldBeOfExactType<CommandDecisionAssertionException>();
    [Fact] void should_show_the_actual_reason() => _error.Message.ShouldContain("validatorFailed");
}
