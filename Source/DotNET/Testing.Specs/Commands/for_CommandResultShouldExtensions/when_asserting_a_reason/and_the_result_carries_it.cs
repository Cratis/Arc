// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Arc.Validation;

namespace Cratis.Arc.Testing.for_CommandResultShouldExtensions.when_asserting_a_reason;

/// <summary>
/// A rejection that carries the reason the spec names satisfies the assertion, and a rejection carrying a different
/// one does not - which is the whole difference between this and ShouldHaveValidationErrors, and the reason it can
/// sit in Screenplay's NamedRejections list.
/// </summary>
[Collection(AssertionPolicyCollection.Name)]
public class and_the_result_carries_it : Specification
{
    CommandResult _raced;
    Exception _wrongReason;
    Exception _rightReason;

    void Establish()
    {
        given.a_recording_policy.Reset();
        _raced = new CommandResult
        {
            ValidationResults = [ValidationResult.Error("Concurrency violation for event source", reason: ValidationResultReason.ConcurrencyViolation)]
        };
    }

    void Because()
    {
        _rightReason = Catch.Exception(() => _raced.ShouldHaveValidationErrorBecauseOf(ValidationResultReason.ConcurrencyViolation));
        _wrongReason = Catch.Exception(() => _raced.ShouldHaveValidationErrorBecauseOf(ValidationResultReason.Rule));
    }

    [Fact] void should_pass_for_the_reason_the_result_carries() => _rightReason.ShouldBeNull();
    [Fact] void should_fail_for_a_reason_it_does_not() => _wrongReason.ShouldBeOfExactType<CommandResultAssertionException>();
    [Fact] void should_say_what_the_result_actually_carried() => _wrongReason.Message.ShouldContain("concurrencyViolation");

    /// <summary>
    /// The undiscriminating assertion passes on the very same result, which is what makes it unable to tell a
    /// retryable race from a rule the spec is named after.
    /// </summary>
    [Fact] void should_be_a_result_the_undiscriminating_assertion_also_passes() => Catch.Exception(_raced.ShouldHaveValidationErrors).ShouldBeNull();
}
