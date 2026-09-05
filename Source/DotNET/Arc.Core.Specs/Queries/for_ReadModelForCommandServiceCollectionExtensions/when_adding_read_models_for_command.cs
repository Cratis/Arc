// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Queries.for_ReadModelForCommandServiceCollectionExtensions;

public class when_adding_read_models_for_command : Specification
{
    IServiceCollection _services;
    ICanResolveReadModelForCommand _resolver;
    RegisteredReadModelTypes _registeredReadModelTypes;

    void Establish()
    {
        _services = new ServiceCollection();
        _resolver = new a_read_model_resolver([typeof(FirstReadModel)]);
    }

    void Because()
    {
        _services.AddReadModelsForCommand(_resolver);
        _registeredReadModelTypes = _services
            .First(descriptor => descriptor.ServiceType == typeof(RegisteredReadModelTypes))
            .ImplementationInstance as RegisteredReadModelTypes;
    }

    [Fact] void should_register_the_provider_resolver() =>
        _services.ShouldContain(descriptor =>
            descriptor.ServiceType == typeof(ICanResolveReadModelForCommand) &&
            descriptor.ImplementationInstance == _resolver &&
            descriptor.Lifetime == ServiceLifetime.Singleton);

    [Fact] void should_register_a_scoped_resolver_for_the_read_model() =>
        _services.ShouldContain(descriptor => descriptor.ServiceType == typeof(FirstReadModel) && descriptor.Lifetime == ServiceLifetime.Scoped);

    [Fact] void should_register_the_read_model_type_as_a_known_read_model() => _registeredReadModelTypes.Contains(typeof(FirstReadModel)).ShouldBeTrue();
}
