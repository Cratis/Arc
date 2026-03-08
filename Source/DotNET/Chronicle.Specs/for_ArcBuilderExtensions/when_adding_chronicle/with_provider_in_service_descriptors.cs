// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.for_ArcBuilderExtensions.when_adding_chronicle;

public class with_provider_in_service_descriptors : given.an_arc_builder
{
    IClientArtifactsProvider _registeredProvider;

    void Establish()
    {
        _registeredProvider = Substitute.For<IClientArtifactsProvider>();
        _registeredProvider.Projections.Returns([]);
        _registeredProvider.ModelBoundProjections.Returns([]);
        _services.AddSingleton(_registeredProvider);
    }

    void Because() => _builder.WithChronicle();

    [Fact] void should_initialize_the_registered_provider() => _registeredProvider.Received(1).Initialize();
}
