// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;
using FluentValidation;

namespace Cratis.Arc.Validation.for_ModelGraphValidator.when_validating;

/// <summary>
/// IgnoreConceptRules() is recorded on the flagging validator instance, not on the concept type itself. A second,
/// unrelated command carrying the same concept type — with no flag of its own — must still get the cross-cutting
/// check, proving the suppression never leaks beyond the validator that declared it.
/// </summary>
public class and_the_same_concept_type_is_used_by_a_different_command : given.a_model_graph_validator
{
    const string ConceptInvalidMessage = "concept invalid";

    record TestConcept(string Value) : ConceptAs<string>(Value);

    record FlaggingCommand(TestConcept Reference);

    record OtherCommand(TestConcept Reference);

    class FlaggingCommandValidator : BaseValidator<FlaggingCommand>
    {
        public FlaggingCommandValidator() => RuleFor(x => x.Reference).IgnoreConceptRules().NotEmpty();
    }

    class AlwaysFailingConceptValidator : AbstractValidator<TestConcept>
    {
        public AlwaysFailingConceptValidator() => RuleFor(x => x.Value).Must(_ => false).WithMessage(ConceptInvalidMessage);
    }

    IEnumerable<ValidationResult> _flaggingCommandResults;
    IEnumerable<ValidationResult> _otherCommandResults;

    void Establish()
    {
        WithValidatorFor(typeof(FlaggingCommand), new FlaggingCommandValidator());
        WithValidatorFor(typeof(TestConcept), new AlwaysFailingConceptValidator());
    }

    async Task Because()
    {
        _flaggingCommandResults = await _validator.Validate(new ModelGraphValidationRequest(new FlaggingCommand(new("x"))));
        _otherCommandResults = await _validator.Validate(new ModelGraphValidationRequest(new OtherCommand(new("y"))));
    }

    [Fact]
    void should_not_fail_the_flagged_command() =>
        _flaggingCommandResults.ShouldNotContain(result => result.Message == ConceptInvalidMessage);

    [Fact]
    void should_still_fail_the_other_command() =>
        _otherCommandResults.ShouldContain(result => result.Message == ConceptInvalidMessage && result.Members.Contains("reference"));
}
