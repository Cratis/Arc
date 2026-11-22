// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Types;
using FluentValidation;

namespace Cratis.Arc.Validation.for_DiscoverableValidators.when_checking_for_a_validator;

public class and_none_is_discovered : Specification
{
    Func<IServiceProvider> _serviceProviderAccessor;
    bool _result;
    DiscoverableValidators _validators;

    void Establish()
    {
        var types = Substitute.For<ITypes>();
        types.FindMultiple(typeof(IValidator)).Returns([]);
        _serviceProviderAccessor = Substitute.For<Func<IServiceProvider>>();
        _validators = new DiscoverableValidators(types, _serviceProviderAccessor);
    }

    void Because() => _result = _validators.HasValidatorFor(typeof(CommandWithDependency));

    [Fact] void should_report_no_validator_is_available() => _result.ShouldBeFalse();
    [Fact] void should_not_access_the_service_provider() => _serviceProviderAccessor.DidNotReceive().Invoke();
}
