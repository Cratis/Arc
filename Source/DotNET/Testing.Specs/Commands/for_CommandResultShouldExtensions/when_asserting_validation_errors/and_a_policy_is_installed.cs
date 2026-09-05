// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Arc.Validation;

namespace Cratis.Arc.Testing.for_CommandResultShouldExtensions.when_asserting_validation_errors;

/// <summary>
/// What the seam exists for: a policy installed once, in one visible place, runs at every call site without any of
/// them being edited.
/// </summary>
[Collection(AssertionPolicyCollection.Name)]
public class and_a_policy_is_installed : Specification
{
    CommandResult _result;
    Exception _error;

    void Establish()
    {
        given.a_recording_policy.Reset();
        _result = new CommandResult { ValidationResults = [ValidationResult.Error("nope")] };
    }

    void Because() => _error = Catch.Exception(_result.ShouldHaveValidationErrors);

    [Fact] void should_not_fail_the_assertion() => _error.ShouldBeNull();
    [Fact] void should_consult_the_policy() => given.a_recording_policy.Consulted.ShouldContainOnly("ShouldHaveValidationErrors");
    [Fact] void should_hand_it_the_result_that_was_asserted() => given.a_recording_policy.Received.ShouldContainOnly(_result);
}
