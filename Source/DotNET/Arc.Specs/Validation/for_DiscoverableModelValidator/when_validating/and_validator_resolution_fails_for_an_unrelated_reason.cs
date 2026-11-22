// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentValidation;

namespace Cratis.Arc.Validation.for_DiscoverableModelValidator.when_validating;

/// <summary>
/// Only the missing-command-context failure is translated into the actionable error. Any other failure must surface
/// unchanged so genuine misconfigurations are not masked as a read-model problem.
/// </summary>
public class and_validator_resolution_fails_for_an_unrelated_reason : given.a_lazy_discoverable_model_validator
{
    Exception _exception;
    UnrelatedFailure _thrown;

    void Establish()
    {
        _thrown = new UnrelatedFailure();
        _discoverableValidators
            .TryGet(typeof(TestModel), _requestServices, out Arg.Any<IValidator>())
            .Returns(_ => throw _thrown);
    }

    void Because() => _exception = Catch.Exception(() => _modelValidator.Validate(_validationContext));

    [Fact] void should_propagate_the_original_exception() => _exception.ShouldEqual(_thrown);

    public class UnrelatedFailure() : Exception("Something unrelated went wrong.");
}
