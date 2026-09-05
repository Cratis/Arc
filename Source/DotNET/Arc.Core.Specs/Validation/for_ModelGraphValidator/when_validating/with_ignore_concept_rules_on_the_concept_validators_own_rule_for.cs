// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;
using FluentValidation;

namespace Cratis.Arc.Validation.for_ModelGraphValidator.when_validating;

/// <summary>
/// RuleFor(_ => _) targets the concept itself, so there is no containing property to key an exclusion by —
/// calling IgnoreConceptRules() there is a documented no-op rather than an error, and must not suppress the
/// validator's own invocation for itself.
/// </summary>
public class with_ignore_concept_rules_on_the_concept_validators_own_rule_for : given.a_model_graph_validator
{
    record TestConcept(string Value) : ConceptAs<string>(Value);

    record Command(TestConcept Reference);

    class SelfIgnoringConceptValidator : BaseValidator<TestConcept>
    {
        public SelfIgnoringConceptValidator() => RuleFor(_ => _).IgnoreConceptRules().Must(_ => false).WithMessage("concept invalid");
    }

    IEnumerable<ValidationResult> _results;

    void Establish() => WithValidatorFor(typeof(TestConcept), new SelfIgnoringConceptValidator());

    async Task Because() => _results = await _validator.Validate(new ModelGraphValidationRequest(new Command(new("x"))));

    [Fact] void should_still_run_the_concept_validator() => _results.ShouldContain(result => result.Message == "concept invalid");
}
