// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.ReadModels.for_ReadModelServiceCollectionExtensions;

public class when_artifacts_change_between_containers : Specification
{
    IClientArtifactsProvider _artifacts;
    ChronicleReadModelForCommandResolver _first;
    ChronicleReadModelForCommandResolver _shared;
    ChronicleReadModelForCommandResolver _changed;

    void Establish()
    {
        _artifacts = Substitute.For<IClientArtifactsProvider>();
        _artifacts.Projections.Returns([]);
        _artifacts.Reducers.Returns([]);
        _artifacts.ModelBoundProjections.Returns([typeof(First)]);
    }

    void Because()
    {
        _first = Register();
        _shared = Register();
        _artifacts.ModelBoundProjections.Returns([typeof(Second)]);
        _changed = Register();
    }

    [Fact] void should_reuse_discovery_for_unchanged_artifacts() => _shared.ReadModelTypes.ShouldBeSame(_first.ReadModelTypes);
    [Fact] void should_keep_the_first_containers_read_models() => _first.ReadModelTypes.ShouldContainOnly(typeof(First));
    [Fact] void should_discover_changed_artifacts() => _changed.ReadModelTypes.ShouldContainOnly(typeof(Second));

    ChronicleReadModelForCommandResolver Register()
    {
        using var provider = new ServiceCollection().AddReadModels(_artifacts).BuildServiceProvider();
        return provider.GetServices<Cratis.Arc.Queries.ICanResolveReadModelForCommand>()
            .OfType<ChronicleReadModelForCommandResolver>().Single();
    }

    record First;
    record Second;
}
