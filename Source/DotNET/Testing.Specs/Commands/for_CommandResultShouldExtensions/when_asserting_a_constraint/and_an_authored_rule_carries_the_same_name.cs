// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Arc.Validation;

namespace Cratis.Arc.Testing.for_CommandResultShouldExtensions.when_asserting_a_constraint;

/// <summary>
/// The detail is a general field, open to every reason, so a name on its own proves nothing. A rule the application
/// authored may label its rejection with anything at all - including a string that happens to read like a constraint
/// name - and that is not the store rejecting the append, which is what the spec claims when it names a constraint.
/// </summary>
[Collection(AssertionPolicyCollection.Name)]
public class and_an_authored_rule_carries_the_same_name : Specification
{
    CommandResult _rejected;
    Exception _error;

    void Establish()
    {
        given.a_recording_policy.Reset();
        _rejected = new CommandResult
        {
            ValidationResults = [ValidationResult.Error("The organization number is already in use", reasonDetail: "UniqueOrganizationNumber")]
        };
    }

    void Because() => _error = Catch.Exception(() => _rejected.ShouldHaveConstraintViolationFor("UniqueOrganizationNumber"));

    [Fact] void should_not_accept_a_rule_wearing_a_constraint_name() => _error.ShouldBeOfExactType<CommandResultAssertionException>();
    [Fact] void should_say_the_rejection_came_from_a_rule() => _error.Message.ShouldContain("rule");
    [Fact] void should_not_consult_the_policy() => given.a_recording_policy.Consulted.ShouldBeEmpty();
}
