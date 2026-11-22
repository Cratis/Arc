// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Types;
using FluentValidation;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Validation.for_DiscoverableModelValidator.when_validating;

public class with_a_disposed_fallback_provider : given.a_discoverable_model_validator
{
    ServiceProvider _fallbackProvider;
    ServiceProvider _requestProvider;
    IServiceScope _requestScope;
    IEnumerable<ModelValidationResult> _result;

    void Establish()
    {
        _fallbackProvider = new ServiceCollection().BuildServiceProvider();
        _fallbackProvider.Dispose();
        _requestProvider = new ServiceCollection().AddScoped(_ => new RequestDependency(true)).BuildServiceProvider();
        _requestScope = _requestProvider.CreateScope();
        _actionContext.HttpContext.RequestServices = _requestScope.ServiceProvider;

        var types = Substitute.For<ITypes>();
        types.FindMultiple(typeof(IValidator)).Returns([typeof(TestValidator)]);
        var validators = new DiscoverableValidators(types, () => _fallbackProvider);
        var provider = new DiscoverableModelValidatorProvider(validators);
        var providerContext = new ModelValidatorProviderContext(_validationContext.ModelMetadata, []);
        provider.CreateValidators(providerContext);
        _modelValidator = (DiscoverableModelValidator)providerContext.Results.Single().Validator;
    }

    void Because() => _result = _modelValidator.Validate(_validationContext);

    void Destroy()
    {
        _requestScope.Dispose();
        _requestProvider.Dispose();
    }

    [Fact] void should_validate_with_the_live_request_dependency() => _result.ShouldBeEmpty();

    public record RequestDependency(bool IsAllowed);

    public class TestValidator : DiscoverableValidator<TestModel>
    {
        public TestValidator(RequestDependency dependency) => RuleFor(_ => _.Value).Must(_ => dependency.IsAllowed);
    }
}
