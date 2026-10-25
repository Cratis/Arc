// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Arc.Validation;

namespace Cratis.Arc.Testing.for_CommandResultShouldExtensions.when_asserting_a_rejection_reason;

[Collection(AssertionPolicyCollection.Name)]
public class and_no_rejection_carries_it : Specification
{
    Exception _error;
    Exception _emptyError;

    void Establish() => given.a_recording_policy.Reset();

    void Because()
    {
        var result = new CommandResult
        {
            ValidationResults = [ValidationResult.Error("Different message.", reason: ValidationResultReason.ConstraintViolation, reasonDetail: "Expected reason.")]
        };
        _error = Catch.Exception(() => result.ShouldBeRejected("Expected reason."));
        _emptyError = Catch.Exception(() => new CommandResult().ShouldBeRejected("Expected reason."));
    }

    [Fact] void should_not_match_the_reason_detail() => _error.ShouldBeOfExactType<CommandResultAssertionException>();
    [Fact] void should_list_reason_detail_and_message() => _error.Message.ShouldContain($"{ValidationResultReason.ConstraintViolation}/Expected reason. ('Different message.')");
    [Fact] void should_explain_an_empty_rejection_list() => _emptyError.Message.ShouldContain("no validation errors at all");
    [Fact] void should_not_consult_the_policy() => given.a_recording_policy.Consulted.ShouldBeEmpty();
}
