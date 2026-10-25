// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Arc.Validation;

namespace Cratis.Arc.Testing.for_CommandResultShouldExtensions.when_asserting_a_rejection_reason;

[Collection(AssertionPolicyCollection.Name)]
public class and_only_a_substring_matches : Specification
{
    CommandResult _result;
    Exception _error;

    void Establish()
    {
        given.a_recording_policy.Reset();
        _result = new() { ValidationResults = [ValidationResult.Error("Balance is insufficient.")] };
    }

    void Because() => _error = Catch.Exception(() => _result.ShouldBeRejected("insufficient"));

    [Fact] void should_fail() => _error.ShouldBeOfExactType<CommandResultAssertionException>();
    [Fact] void should_show_the_actual_message() => _error.Message.ShouldContain("Balance is insufficient.");
    [Fact] void should_not_consult_the_policy() => given.a_recording_policy.Consulted.ShouldBeEmpty();
}
