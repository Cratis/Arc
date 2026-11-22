// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;
using FluentValidation;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Cratis.Arc.Validation.for_DiscoverableModelValidator.when_validating;

public class with_a_lazily_resolved_concept_validator_and_aspnet_result_attribute : given.a_lazy_discoverable_model_validator
{
    IEnumerable<ModelValidationResult> _result;

    void Establish()
    {
        var validator = new TestConceptValidator();
        _discoverableValidators.TryGet(typeof(TestConcept), _requestServices, out Arg.Any<IValidator>())
            .Returns(call =>
            {
                call[2] = validator;
                return true;
            });
        _modelValidator = new DiscoverableModelValidator(typeof(TestConcept), _discoverableValidators);
        _actionDescriptor.FilterDescriptors.Add(new FilterDescriptor(new AspNetResultAttribute(), 0));
        var metadataProvider = new EmptyModelMetadataProvider();
        _validationContext = new ModelValidationContext(_actionContext, metadataProvider.GetMetadataForType(typeof(TestConcept)), metadataProvider, null, new TestConcept(""));
    }

    void Because() => _result = _modelValidator.Validate(_validationContext);

    [Fact] void should_return_no_validation_errors() => _result.ShouldBeEmpty();

    class TestConceptValidator : ConceptValidator<TestConcept>
    {
        public TestConceptValidator() => RuleFor(_ => _.Value).NotEmpty();
    }

    record TestConcept(string Value) : ConceptAs<string>(Value);
}
