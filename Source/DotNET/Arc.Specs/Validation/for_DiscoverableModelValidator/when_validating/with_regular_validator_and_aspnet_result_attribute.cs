// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
using FluentValidation;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Cratis.Arc.Validation.for_DiscoverableModelValidator.when_validating;

public class with_regular_validator_and_aspnet_result_attribute : given.a_discoverable_model_validator
{
    IEnumerable<ModelValidationResult> _result;

    void Establish()
    {
        var regularValidator = new TestRegularValidator();
        _modelValidator = new DiscoverableModelValidator(regularValidator);

        _actionDescriptor.FilterDescriptors.Add(new FilterDescriptor(new AspNetResultAttribute(), 0));

        _actionContext.HttpContext.Request.Method = "POST";
    }

    void Because() => _result = _modelValidator.Validate(_validationContext);

    [Fact] void should_run_validation() => _result.Count().ShouldEqual(1);
    [Fact] void should_have_validation_error_for_value() => _result.First().MemberName.ShouldEqual("Value");

    class TestRegularValidator : BaseValidator<TestModel>
    {
        public TestRegularValidator()
        {
            RuleFor(x => x.Value).NotEmpty().WithMessage("Value is required");
        }
    }
}
