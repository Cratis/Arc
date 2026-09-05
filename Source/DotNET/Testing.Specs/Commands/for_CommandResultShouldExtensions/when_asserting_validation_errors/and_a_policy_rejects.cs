// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Arc.Validation;

namespace Cratis.Arc.Testing.for_CommandResultShouldExtensions.when_asserting_validation_errors;

/// <summary>
/// A policy strengthens by failing an assertion the built-in check would have passed. If its rejection did not
/// surface, installing one would be decorative.
/// </summary>
[Collection(AssertionPolicyCollection.Name)]
public class and_a_policy_rejects : Specification
{
    CommandResult _result;
    Exception _error;

    void Establish()
    {
        given.a_recording_policy.Reset();
        given.a_recording_policy.Rejects = true;
        _result = new CommandResult { ValidationResults = [ValidationResult.Error("nope")] };
    }

    void Because() => _error = Catch.Exception(_result.ShouldHaveValidationErrors);

    void Destroy() => given.a_recording_policy.Reset();

    [Fact] void should_fail_the_assertion() => _error.ShouldBeOfExactType<CommandResultAssertionException>();
    [Fact] void should_fail_with_the_policys_own_message() => _error.Message.ShouldEqual(given.a_recording_policy.RejectionMessage);
}
