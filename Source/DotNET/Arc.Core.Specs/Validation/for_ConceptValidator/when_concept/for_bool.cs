// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentValidation;

namespace Cratis.Arc.Validation.for_ConceptValidator.when_concept;

public class for_bool : Specification
{
    class validator : ConceptValidator<BoolConcept>
    {
        public validator()
        {
            RuleFor(x => x).NotEmpty();
        }
    }
    validator _validator;

    void Because() => _validator = new validator();

    [Fact]
    void should_not_fail_when_validating_non_empty_value() => _validator.Validate(new BoolConcept(true)).IsValid.ShouldBeTrue();
}