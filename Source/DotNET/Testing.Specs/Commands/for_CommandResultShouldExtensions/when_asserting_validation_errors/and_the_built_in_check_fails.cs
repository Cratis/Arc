// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;

namespace Cratis.Arc.Testing.for_CommandResultShouldExtensions.when_asserting_validation_errors;

/// <summary>
/// The ordering pin. A policy strengthens an assertion that passed; it is not a second opinion on one that already
/// failed. Consulting it here would let a policy turn a failing assertion into a passing one - or, more likely,
/// replace the built-in message that says what actually went wrong with the policy's own.
/// </summary>
[Collection(AssertionPolicyCollection.Name)]
public class and_the_built_in_check_fails : Specification
{
    CommandResult _result;
    Exception _error;

    void Establish()
    {
        given.a_recording_policy.Reset();
        _result = new CommandResult();
    }

    void Because() => _error = Catch.Exception(_result.ShouldHaveValidationErrors);

    [Fact] void should_fail_the_assertion() => _error.ShouldBeOfExactType<CommandResultAssertionException>();
    [Fact] void should_keep_the_built_in_message() => _error.Message.ShouldEqual("Expected command to have validation errors, but it had none.");
    [Fact] void should_not_consult_the_policy() => given.a_recording_policy.Consulted.ShouldBeEmpty();
}
