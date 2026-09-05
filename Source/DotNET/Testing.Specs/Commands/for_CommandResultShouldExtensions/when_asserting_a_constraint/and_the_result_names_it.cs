// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Arc.Validation;

namespace Cratis.Arc.Testing.for_CommandResultShouldExtensions.when_asserting_a_constraint;

/// <summary>
/// The assertion Chronicle already ships for an append result, now available where the same events reach the store
/// through a command. It separates one constraint from another, which is what the reason assertion cannot do - every
/// constraint in the system satisfies that one equally.
/// </summary>
[Collection(AssertionPolicyCollection.Name)]
public class and_the_result_names_it : Specification
{
    CommandResult _rejected;
    Exception _rightConstraint;
    Exception _wrongConstraint;

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

    void Because()
    {
        _rightConstraint = Catch.Exception(() => _rejected.ShouldHaveConstraintViolationFor("UniqueOrganizationNumber"));
        _wrongConstraint = Catch.Exception(() => _rejected.ShouldHaveConstraintViolationFor("UniqueEmailAddress"));
    }

    [Fact] void should_pass_for_the_constraint_the_result_names() => _rightConstraint.ShouldBeNull();
    [Fact] void should_fail_for_a_constraint_it_does_not() => _wrongConstraint.ShouldBeOfExactType<CommandResultAssertionException>();
    [Fact] void should_say_which_constraint_was_actually_violated() => _wrongConstraint.Message.ShouldContain("UniqueOrganizationNumber");
    [Fact] void should_consult_the_policy_for_the_assertion_that_passed() => given.a_recording_policy.Consulted.ShouldContainOnly(nameof(CommandResultShouldExtensions.ShouldHaveConstraintViolationFor));

    /// <summary>
    /// The reason assertion passes on the very same result no matter which constraint the spec meant, so it cannot
    /// tell a spec named after one uniqueness rule from a rejection produced by an entirely different one.
    /// </summary>
    [Fact] void should_be_a_result_the_reason_assertion_cannot_tell_apart() =>
        Catch.Exception(() => _rejected.ShouldHaveValidationErrorBecauseOf(ValidationResultReason.ConstraintViolation)).ShouldBeNull();
}
