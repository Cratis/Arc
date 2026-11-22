// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentValidation;

namespace Cratis.Arc.Validation.for_DiscoverableModelValidator.when_validating;

public class and_the_validator_is_no_longer_available : given.a_lazy_discoverable_model_validator
{
    Exception _exception;

    void Establish() => _discoverableValidators.TryGet(typeof(TestModel), _requestServices, out Arg.Any<IValidator>()).Returns(false);

    void Because() => _exception = Catch.Exception(() => _modelValidator.Validate(_validationContext));

    [Fact] void should_throw_a_missing_validator_error() => _exception.ShouldBeOfExactType<DiscoverableModelValidatorNotFound>();
    [Fact] void should_identify_the_model_type() => _exception.Message.ShouldContain(typeof(TestModel).FullName);
    [Fact] void should_not_validate() => _validator.DidNotReceive().ValidateAsync(Arg.Any<IValidationContext>(), Arg.Any<CancellationToken>());
}
