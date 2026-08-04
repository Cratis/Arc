// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Arc.Validation;

namespace Cratis.Arc.Testing.for_CommandResultShouldExtensions.when_asserting_validation_errors;

/// <summary>
/// The false green this closes. A validator whose constructor asks for a read model the spec forgot to seed is never
/// built, so not one of its rules runs - and the command is rejected anyway, by the pipeline. The spec passes, and
/// would keep passing with the rule it is named after deleted.
/// </summary>
/// <remarks>
/// Only when the dependency failure is the <em>whole</em> story. A result carrying a real rule rejection alongside
/// it has a rule that ran, so the assertion has something to be about and stands.
/// </remarks>
[Collection(AssertionPolicyCollection.Name)]
public class and_only_a_dependency_rejected : Specification
{
    CommandResult _onlyTheDependency;
    CommandResult _aRuleRejectedToo;
    Exception _dependencyOnly;
    Exception _withARule;

    void Establish()
    {
        given.a_recording_policy.Reset();
        _onlyTheDependency = new CommandResult
        {
            ValidationResults = [ValidationResult.Error(
                "The command targets an entity that does not exist.",
                reason: ValidationResultReason.DependencyUnavailable)]
        };
        _aRuleRejectedToo = new CommandResult
        {
            ValidationResults =
            [
                ValidationResult.Error("The command targets an entity that does not exist.", reason: ValidationResultReason.DependencyUnavailable),
                ValidationResult.Error("Contracts must be signed.")
            ]
        };
    }

    void Because()
    {
        _dependencyOnly = Catch.Exception(_onlyTheDependency.ShouldHaveValidationErrors);
        _withARule = Catch.Exception(_aRuleRejectedToo.ShouldHaveValidationErrors);
    }

    [Fact] void should_refuse_to_pass_on_the_dependency_alone() => _dependencyOnly.ShouldBeOfExactType<CommandResultAssertionException>();
    [Fact] void should_say_no_rule_ever_ran() => _dependencyOnly.Message.ShouldContain("no rule ever ran");
    [Fact] void should_point_at_the_way_to_assert_it_deliberately() => _dependencyOnly.Message.ShouldContain(nameof(CommandResultShouldExtensions.ShouldHaveValidationErrorBecauseOf));
    [Fact] void should_still_pass_when_a_rule_rejected_as_well() => _withARule.ShouldBeNull();

    /// <summary>
    /// The escape hatch has to work, or a spec that genuinely means to assert the case has nowhere to go.
    /// </summary>
    [Fact] void should_let_a_spec_assert_the_case_deliberately() =>
        Catch.Exception(() => _onlyTheDependency.ShouldHaveValidationErrorBecauseOf(ValidationResultReason.DependencyUnavailable)).ShouldBeNull();
}
