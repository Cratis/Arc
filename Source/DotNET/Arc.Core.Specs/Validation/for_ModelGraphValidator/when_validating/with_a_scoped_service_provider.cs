// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentValidation;

namespace Cratis.Arc.Validation.for_ModelGraphValidator.when_validating;

/// <summary>
/// A validator can depend on something scoped to the request — a read model resolved for the current tenant, for
/// example. Resolving it from the ambient container instead would either fail scope validation or hand the validator
/// state belonging to a different request.
/// </summary>
public class with_a_scoped_service_provider : given.a_model_graph_validator
{
    IServiceProvider _serviceProvider;
    IServiceProvider _providerUsed;

    void Establish()
    {
        _serviceProvider = Substitute.For<IServiceProvider>();
        _discoverableValidators
            .TryGet(Arg.Any<Type>(), Arg.Any<IServiceProvider>(), out Arg.Any<IValidator>())
            .Returns(x =>
            {
                _providerUsed = (IServiceProvider)x[1];
                return false;
            });
    }

    async Task Because() => await _validator.Validate(new ModelGraphValidationRequest(new Model(), _serviceProvider));

    [Fact] void should_resolve_validators_from_the_supplied_provider() => _providerUsed.ShouldEqual(_serviceProvider);

    record Model;
}
