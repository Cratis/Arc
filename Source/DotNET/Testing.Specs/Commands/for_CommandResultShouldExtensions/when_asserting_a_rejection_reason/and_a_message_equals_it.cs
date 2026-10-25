// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Arc.Validation;

namespace Cratis.Arc.Testing.for_CommandResultShouldExtensions.when_asserting_a_rejection_reason;

[Collection(AssertionPolicyCollection.Name)]
public class and_a_message_equals_it : Specification
{
    CommandResult _result;
    Exception _error;

    void Establish()
    {
        given.a_recording_policy.Reset();
        _result = new() { ValidationResults = [ValidationResult.Error("Other rejection."), ValidationResult.Error("Balance is insufficient.")] };
    }

    void Because() => _error = Catch.Exception(() => _result.ShouldBeRejected("Balance is insufficient."));

    [Fact] void should_match_any_complete_message() => _error.ShouldBeNull();
    [Fact] void should_consult_the_policy() => given.a_recording_policy.Consulted.ShouldContainOnly(nameof(CommandResultShouldExtensions.ShouldBeRejected));
    [Fact] void should_pass_the_result_to_the_policy() => given.a_recording_policy.Received.ShouldContainOnly(_result);
}
