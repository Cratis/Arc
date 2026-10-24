// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Arc.Validation;

namespace Cratis.Arc.Testing.for_CommandResultShouldExtensions.when_asserting_a_constraint;

/// <summary>
/// The reason assertion passes on the very same result no matter which constraint the spec meant, so it cannot
/// tell a spec named after one uniqueness rule from a rejection produced by an entirely different one.
/// </summary>
[Collection(AssertionPolicyCollection.Name)]
public class and_the_reason_assertion_is_used : Specification
{
    CommandResult _rejected;
    Exception _reasonAssertion;

    void Establish()
    {
        given.a_recording_policy.Reset();
        _rejected = new CommandResult
        {
            ValidationResults =
            [
                ValidationResult.Error(
                    "The organization number is already in use",
                    reason: ValidationResultReason.ConstraintViolation,
                    reasonDetail: "UniqueOrganizationNumber")
            ]
        };
    }

    void Because() => _reasonAssertion = Catch.Exception(() => _rejected.ShouldHaveValidationErrorBecauseOf(ValidationResultReason.ConstraintViolation));

    [Fact] void should_be_a_result_the_reason_assertion_cannot_tell_apart() => _reasonAssertion.ShouldBeNull();
}
