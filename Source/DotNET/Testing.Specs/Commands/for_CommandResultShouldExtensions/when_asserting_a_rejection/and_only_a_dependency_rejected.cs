// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Arc.Validation;

namespace Cratis.Arc.Testing.for_CommandResultShouldExtensions.when_asserting_a_rejection;

[Collection(AssertionPolicyCollection.Name)]
public class and_only_a_dependency_rejected : Specification
{
    CommandResult _result;
    Exception _error;

    void Establish()
    {
        given.a_recording_policy.Reset();
        _result = new() { ValidationResults = [ValidationResult.Error("Missing account.", reason: ValidationResultReason.DependencyUnavailable)] };
    }

    void Because() => _error = Catch.Exception(_result.ShouldBeRejected);

    [Fact] void should_fail() => _error.ShouldBeOfExactType<CommandResultAssertionException>();
    [Fact] void should_explain_that_no_rule_ran() => _error.Message.ShouldContain("no rule ever ran");
    [Fact] void should_suggest_the_explicit_dependency_assertion() => _error.Message.ShouldContain("ShouldHaveValidationErrorBecauseOf(ValidationResultReason.DependencyUnavailable)");
    [Fact] void should_not_consult_the_policy() => given.a_recording_policy.Consulted.ShouldBeEmpty();
}
