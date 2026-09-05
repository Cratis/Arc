// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Queries.for_ReadModelForCommandServiceCollectionExtensions;

public class when_adding_read_models_from_two_providers : Specification
{
    IServiceCollection _services;
    RegisteredReadModelTypes _registeredReadModelTypes;

    void Establish() => _services = new ServiceCollection();

    void Because()
    {
        _services.AddReadModelsForCommand(new a_read_model_resolver([typeof(FirstReadModel)]));
        _services.AddReadModelsForCommand(new a_read_model_resolver([typeof(SecondReadModel)]));
        _registeredReadModelTypes = _services
            .First(descriptor => descriptor.ServiceType == typeof(RegisteredReadModelTypes))
            .ImplementationInstance as RegisteredReadModelTypes;
    }

    [Fact] void should_register_a_scoped_resolver_for_the_first_provider_read_model() =>
        _services.ShouldContain(descriptor => descriptor.ServiceType == typeof(FirstReadModel) && descriptor.Lifetime == ServiceLifetime.Scoped);

    [Fact] void should_register_a_scoped_resolver_for_the_second_provider_read_model() =>
        _services.ShouldContain(descriptor => descriptor.ServiceType == typeof(SecondReadModel) && descriptor.Lifetime == ServiceLifetime.Scoped);

    [Fact] void should_keep_the_first_provider_read_model_in_the_known_set() => _registeredReadModelTypes.Contains(typeof(FirstReadModel)).ShouldBeTrue();
    [Fact] void should_add_the_second_provider_read_model_to_the_known_set() => _registeredReadModelTypes.Contains(typeof(SecondReadModel)).ShouldBeTrue();
}
