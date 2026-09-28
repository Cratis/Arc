// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;
using FluentValidation;

namespace Cratis.Arc.Validation.for_BaseValidator;

public class when_validating_concept_as_model : Specification
{
    record TestConcept(string Value) : ConceptAs<string>(Value);

    class TestConceptValidator : BaseValidator<TestConcept>
    {
        public TestConceptValidator() => RuleFor(x => x).NotEmpty();
    }

    FluentValidation.Results.ValidationResult _result;

    void Because() => _result = new TestConceptValidator().Validate(new TestConcept(string.Empty));

    [Fact] void should_reject_the_empty_concept() => _result.IsValid.ShouldBeFalse();
}
