// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;

namespace Cratis.Arc.Chronicle.for_ArcBuilderExtensions.when_adding_chronicle;

public class with_explicit_provider : given.an_arc_builder
{
    IClientArtifactsProvider _provider;

    void Establish()
    {
        _provider = Substitute.For<IClientArtifactsProvider>();
        _provider.Projections.Returns([]);
        _provider.ModelBoundProjections.Returns([]);
    }

    void Because() => _builder.WithChronicle(_provider);

    [Fact] void should_initialize_the_provider() => _provider.Received(1).Initialize();
}
