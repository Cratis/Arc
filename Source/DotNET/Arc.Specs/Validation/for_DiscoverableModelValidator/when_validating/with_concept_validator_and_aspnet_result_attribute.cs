// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
using Cratis.Concepts;
using FluentValidation;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Cratis.Arc.Validation.for_DiscoverableModelValidator.when_validating;

public class with_concept_validator_and_aspnet_result_attribute : given.a_discoverable_model_validator
{
    IEnumerable<ModelValidationResult> _result;
    TestConcept _conceptModel;

    void Establish()
    {
        var conceptValidator = new TestConceptValidator();
        _modelValidator = new DiscoverableModelValidator(conceptValidator);

        _actionDescriptor.FilterDescriptors.Add(new FilterDescriptor(new AspNetResultAttribute(), 0));

        _conceptModel = new TestConcept("");
        var modelMetadataProvider = new Microsoft.AspNetCore.Mvc.ModelBinding.EmptyModelMetadataProvider();
        var conceptMetadata = modelMetadataProvider.GetMetadataForType(typeof(TestConcept));
        _validationContext = new ModelValidationContext(_actionContext, conceptMetadata, modelMetadataProvider, null, _conceptModel);
    }

    void Because() => _result = _modelValidator.Validate(_validationContext);

    [Fact] void should_return_no_validation_errors() => _result.ShouldBeEmpty();

    class TestConceptValidator : ConceptValidator<TestConcept>
    {
        public TestConceptValidator()
        {
            RuleFor(x => x.Value).NotEmpty();
        }
    }

    record TestConcept(string Value) : ConceptAs<string>(Value)
    {
        public static implicit operator TestConcept(string value) => new(value);
    }
}
