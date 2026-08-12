// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;

namespace Cratis.Arc.Testing.for_CommandResultShouldExtensions.when_asserting_a_constraint;

/// <summary>
/// A command that succeeded fails the assertion, and says so in the terms the reader needs. Listing the rejections
/// that did happen is useless when there were none, so the diagnostic states the absence instead of trailing off
/// after "Actual:" and leaving the reader to guess whether the constraint fired under another name.
/// </summary>
[Collection(AssertionPolicyCollection.Name)]
public class and_nothing_rejected_the_command : Specification
{
    CommandResult _successful;
    Exception _error;

    void Establish()
    {
        given.a_recording_policy.Reset();
        _successful = new CommandResult();
    }

    void Because() => _error = Catch.Exception(() => _successful.ShouldHaveConstraintViolationFor("UniqueOrganizationNumber"));

    [Fact] void should_fail_the_assertion() => _error.ShouldBeOfExactType<CommandResultAssertionException>();
    [Fact] void should_say_there_were_no_validation_errors_at_all() => _error.Message.ShouldContain("no validation errors at all");
    [Fact] void should_not_consult_the_policy() => given.a_recording_policy.Consulted.ShouldBeEmpty();
}
