// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
using Cratis.Monads;

namespace Cratis.Arc.Testing.Commands.for_CommandRejectionShouldExtensions.when_asserting_a_validation_reason;

public class and_the_reason_matches : Specification
{
    ValidationResult _rejection = ValidationResult.Error("Unavailable", reason: ValidationResultReason.DependencyUnavailable);
    ValidationResult _matched;
    void Because()
    {
        Result<string, ValidationResult> result = _rejection;
        _matched = result.ShouldHaveValidationErrorBecauseOf(ValidationResultReason.DependencyUnavailable);
    }
    [Fact] void should_return_the_rejection() => _matched.ShouldEqual(_rejection);
}
