// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;
using FluentValidation;

namespace Cratis.Arc.Validation.for_ModelGraphValidator.when_validating;

/// <summary>
/// A validator can suppress the cross-cutting concept validator for one of its own properties via
/// <c>RuleFor(...).IgnoreConceptRules()</c> — typically because that property names an entity the command itself
/// is creating, rather than referencing one that must already exist. The suppression must be scoped to exactly
/// that property: a sibling property of the same concept type, with no flag of its own, still gets the
/// cross-cutting check.
/// </summary>
public class with_a_property_flagged_to_ignore_concept_rules : given.a_model_graph_validator
{
    const string ConceptInvalidMessage = "concept invalid";

    record TestConcept(string Value) : ConceptAs<string>(Value);

    record Command(TestConcept Flagged, TestConcept Unflagged);

    class CommandValidator : BaseValidator<Command>
    {
        public CommandValidator() => RuleFor(x => x.Flagged).IgnoreConceptRules().NotEmpty();
    }

    class AlwaysFailingConceptValidator : AbstractValidator<TestConcept>
    {
        public AlwaysFailingConceptValidator() => RuleFor(x => x.Value).Must(_ => false).WithMessage(ConceptInvalidMessage);
    }

    IEnumerable<ValidationResult> _results;

    void Establish()
    {
        WithValidatorFor(typeof(Command), new CommandValidator());
        WithValidatorFor(typeof(TestConcept), new AlwaysFailingConceptValidator());
    }

    async Task Because() => _results = await _validator.Validate(new ModelGraphValidationRequest(new Command(new("x"), new("y"))));

    [Fact]
    void should_not_fail_the_flagged_property_from_the_concept_validator() =>
        _results.ShouldNotContain(result => result.Message == ConceptInvalidMessage && result.Members.Contains("flagged"));

    [Fact]
    void should_still_fail_the_unflagged_sibling_from_the_concept_validator() =>
        _results.ShouldContain(result => result.Message == ConceptInvalidMessage && result.Members.Contains("unflagged"));
}
