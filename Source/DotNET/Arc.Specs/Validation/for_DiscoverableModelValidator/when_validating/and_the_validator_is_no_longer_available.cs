// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentValidation;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Cratis.Arc.Validation.for_DiscoverableModelValidator.when_validating;

public class and_the_validator_is_no_longer_available : given.a_lazy_discoverable_model_validator
{
    IEnumerable<ModelValidationResult> _result;

    void Establish() => _discoverableValidators.TryGet(typeof(TestModel), _requestServices, out Arg.Any<IValidator>()).Returns(false);

    void Because() => _result = _modelValidator.Validate(_validationContext);

    [Fact] void should_return_no_validation_errors() => _result.ShouldBeEmpty();
    [Fact] void should_not_validate() => _validator.DidNotReceive().ValidateAsync(Arg.Any<IValidationContext>(), Arg.Any<CancellationToken>());
}
