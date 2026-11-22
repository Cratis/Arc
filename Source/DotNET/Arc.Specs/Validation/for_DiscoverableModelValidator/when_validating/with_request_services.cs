// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Cratis.Arc.Validation.for_DiscoverableModelValidator.when_validating;

public class with_request_services : given.a_lazy_discoverable_model_validator
{
    IEnumerable<ModelValidationResult> _result;

    void Establish() => _validator.ValidateAsync(Arg.Any<IValidationContext>(), Arg.Any<CancellationToken>())
        .Returns(new ValidationResult([new ValidationFailure(nameof(TestModel.Value), "Invalid value")]));

    void Because() => _result = _modelValidator.Validate(_validationContext);

    [Fact] void should_resolve_from_request_services() => _discoverableValidators.Received(1).TryGet(typeof(TestModel), _requestServices, out Arg.Any<IValidator>());
    [Fact] void should_not_resolve_from_the_fallback_provider() => _discoverableValidators.DidNotReceive().TryGet(typeof(TestModel), out Arg.Any<IValidator>());
    [Fact] void should_preserve_the_validation_failure_member() => _result.Single().MemberName.ShouldEqual(nameof(TestModel.Value));
    [Fact] void should_preserve_the_validation_failure_message() => _result.Single().Message.ShouldEqual("Invalid value");
}
