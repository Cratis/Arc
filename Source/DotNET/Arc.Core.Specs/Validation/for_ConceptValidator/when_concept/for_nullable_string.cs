// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentValidation;

namespace Cratis.Arc.Validation.for_ConceptValidator.when_concept;

public class for_nullable_string : Specification
{
    class model
    {
        public StringConcept? Value { get; set; }
    }

    class validator : AbstractValidator<model>
    {
        public validator()
        {
            RuleFor(x => x.Value).NotNull();
        }
    }

    validator _validator;
    FluentValidation.Results.ValidationResult _result;

    void Establish() => _validator = new validator();

    void Because() => _result = _validator.Validate(new model { Value = null });

    [Fact]
    void should_fail_when_validating_object_with_null_property() => _result.IsValid.ShouldBeFalse();

    [Fact]
    void should_have_validation_error_for_value() => _result.Errors.ShouldContain(e => e.PropertyName.Equals("value", StringComparison.OrdinalIgnoreCase));

    [Fact]
    void should_not_fail_when_validating_object_with_non_empty_value() => _validator.Validate(new model { Value = new StringConcept("hello") }).IsValid.ShouldBeTrue();
}
