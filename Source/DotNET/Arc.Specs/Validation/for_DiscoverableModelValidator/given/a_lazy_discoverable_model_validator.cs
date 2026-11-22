// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentValidation;
using FluentValidation.Results;

namespace Cratis.Arc.Validation.for_DiscoverableModelValidator.given;

public class a_lazy_discoverable_model_validator : a_discoverable_model_validator
{
    protected IDiscoverableValidators _discoverableValidators;
    protected IServiceProvider _requestServices;

    void Establish()
    {
        _requestServices = Substitute.For<IServiceProvider>();
        _actionContext.HttpContext.RequestServices = _requestServices;
        _discoverableValidators = Substitute.For<IDiscoverableValidators>();
        _discoverableValidators.TryGet(typeof(TestModel), _requestServices, out Arg.Any<IValidator>())
            .Returns(call =>
            {
                call[2] = _validator;
                return true;
            });
        _validator.ValidateAsync(Arg.Any<IValidationContext>(), Arg.Any<CancellationToken>()).Returns(new ValidationResult());
        _modelValidator = new DiscoverableModelValidator(typeof(TestModel), _discoverableValidators);
    }
}
