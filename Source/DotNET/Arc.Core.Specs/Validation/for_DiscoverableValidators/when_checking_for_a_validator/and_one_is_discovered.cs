// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Types;

namespace Cratis.Arc.Validation.for_DiscoverableValidators.when_checking_for_a_validator;

public class and_one_is_discovered : Specification
{
    Func<IServiceProvider> _serviceProviderAccessor;
    bool _result;
    DiscoverableValidators _validators;

    void Establish()
    {
        var types = Substitute.For<ITypes>();
        types.FindMultiple(typeof(IDiscoverableValidator<>)).Returns([typeof(CommandWithDependencyValidator)]);
        _serviceProviderAccessor = Substitute.For<Func<IServiceProvider>>();
        _validators = new DiscoverableValidators(types, _serviceProviderAccessor);
    }

    void Because() => _result = _validators.HasValidatorFor(typeof(CommandWithDependency));

    [Fact] void should_report_the_validator_is_available() => _result.ShouldBeTrue();
    [Fact] void should_not_access_the_service_provider_or_construct_the_validator() => _serviceProviderAccessor.DidNotReceive().Invoke();
}
