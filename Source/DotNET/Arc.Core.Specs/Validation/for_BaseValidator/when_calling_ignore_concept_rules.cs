// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;
using FluentValidation;

namespace Cratis.Arc.Validation.for_BaseValidator;

public class when_calling_ignore_concept_rules : Specification
{
    record TestConcept(string Value) : ConceptAs<string>(Value);

    record TestCommand(TestConcept Flagged, TestConcept Unflagged);

    class TestCommandValidator : BaseValidator<TestCommand>
    {
        public TestCommandValidator()
        {
            RuleFor(x => x.Flagged).IgnoreConceptRules().NotEmpty();
            RuleFor(x => x.Unflagged).NotEmpty();
        }
    }

    TestCommandValidator _validator;
    FluentValidation.Results.ValidationResult _result;

    void Establish() => _validator = new TestCommandValidator();

    void Because() => _result = _validator.Validate(new TestCommand(new(string.Empty), new(string.Empty)));

    [Fact] void should_record_the_flagged_property() => _validator.IgnoredConceptRuleMembers.ShouldContain("flagged");
    [Fact] void should_not_record_the_unflagged_property() => _validator.IgnoredConceptRuleMembers.ShouldNotContain("unflagged");
    [Fact] void should_still_apply_the_validators_own_rule_for_the_flagged_property() => _result.Errors.ShouldContain(error => error.PropertyName == "flagged");
    [Fact] void should_still_apply_the_validators_own_rule_for_the_unflagged_property() => _result.Errors.ShouldContain(error => error.PropertyName == "unflagged");
}
