// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;
using Cratis.Chronicle;
using Cratis.Chronicle.Projections.ModelBound;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.ReadModels.for_ReadModelServiceCollectionExtensions;

public class when_model_bound_children_are_registered_elsewhere : Specification
{
    IServiceCollection _services;
    IClientArtifactsProvider _artifacts;
    ICanResolveReadModelForCommand _otherResolver;
    ServiceDescriptor _otherRegistration;
    ApplicationChild _applicationChild;
    ServiceProvider _provider;

    void Establish()
    {
        _services = new ServiceCollection();
        _otherResolver = Substitute.For<ICanResolveReadModelForCommand>();
        _otherResolver.ReadModelTypes.Returns([typeof(ProviderChild)]);
        _otherResolver.Ownership.Returns(ReadModelForCommandOwnership.Declared);
        _services.AddReadModelsForCommand(_otherResolver);
        _otherRegistration = _services.Single(_ => _.ServiceType == typeof(ProviderChild));
        _applicationChild = new("Application-owned");
        _services.AddSingleton(_applicationChild);

        _artifacts = Substitute.For<IClientArtifactsProvider>();
        _artifacts.Projections.Returns([]);
        _artifacts.Reducers.Returns([]);
        _artifacts.ModelBoundProjections.Returns([typeof(Parent), typeof(ProviderChild), typeof(ApplicationChild)]);
    }

    void Because()
    {
        _services.AddReadModels(_artifacts);
        _provider = _services.BuildServiceProvider();
    }

    void Destroy() => _provider.Dispose();

    [Fact] void should_preserve_the_other_providers_service_registration() => _services.Single(_ => _.ServiceType == typeof(ProviderChild)).ShouldEqual(_otherRegistration);
    [Fact] void should_preserve_the_other_resolver() => _provider.GetServices<ICanResolveReadModelForCommand>().ShouldContain(_otherResolver);
    [Fact] void should_preserve_the_application_instance() => _provider.GetRequiredService<ApplicationChild>().ShouldEqual(_applicationChild);
    [Fact] void should_preserve_the_application_registration_lifetime() => _services.Single(_ => _.ServiceType == typeof(ApplicationChild)).Lifetime.ShouldEqual(ServiceLifetime.Singleton);
    [Fact] void should_keep_the_other_providers_types_in_the_additive_registry() => _provider.GetRequiredService<RegisteredReadModelTypes>().Types.ShouldContainOnly(typeof(ProviderChild), typeof(Parent));
    [Fact] void should_not_claim_either_child_for_chronicle() => _provider.GetServices<ICanResolveReadModelForCommand>().OfType<ChronicleReadModelForCommandResolver>().Single().ReadModelTypes.ShouldContainOnly(typeof(Parent));

    record Parent(
        [ChildrenFrom<ChildAdded>(key: nameof(ChildAdded.Id))] IEnumerable<ProviderChild> ProviderChildren,
        [ChildrenFrom<ChildAdded>(key: nameof(ChildAdded.Id))] IEnumerable<ApplicationChild> ApplicationChildren);

    record ProviderChild(string Name);
    record ApplicationChild(string Name);
    record ChildAdded(Guid Id);
}
