// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;
using FluentValidation;

namespace Cratis.Arc.Validation.for_ModelGraphValidator.when_validating;

/// <summary>
/// The exclusion a validator declares always applies to its own direct properties, regardless of how deep that
/// validator's model sits within a larger graph — not just when it happens to be the graph root.
/// </summary>
public class with_a_property_flagged_two_levels_deep : given.a_model_graph_validator
{
    const string ConceptInvalidMessage = "concept invalid";

    record TestConcept(string Value) : ConceptAs<string>(Value);

    record Branch(TestConcept Concept);

    record Root(Branch Branch);

    class BranchValidator : BaseValidator<Branch>
    {
        public BranchValidator() => RuleFor(x => x.Concept).IgnoreConceptRules().NotEmpty();
    }

    class AlwaysFailingConceptValidator : AbstractValidator<TestConcept>
    {
        public AlwaysFailingConceptValidator() => RuleFor(x => x.Value).Must(_ => false).WithMessage(ConceptInvalidMessage);
    }

    IEnumerable<ValidationResult> _results;

    void Establish()
    {
        WithValidatorFor(typeof(Branch), new BranchValidator());
        WithValidatorFor(typeof(TestConcept), new AlwaysFailingConceptValidator());
    }

    async Task Because() => _results = await _validator.Validate(new ModelGraphValidationRequest(new Root(new Branch(new("x")))));

    [Fact]
    void should_not_fail_the_flagged_property_from_the_concept_validator() =>
        _results.ShouldNotContain(result => result.Message == ConceptInvalidMessage);
}
