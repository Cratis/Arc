// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;
using Cratis.Chronicle;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.ReadModels.for_ReadModelServiceCollectionExtensions;

public class when_self_reducer_artifacts_change : Specification
{
    IClientArtifactsProvider _artifacts;
    IEnumerable<Type> _first;
    IEnumerable<Type> _unchanged;
    IEnumerable<Type> _changed;

    void Establish()
    {
        _artifacts = Substitute.For<IClientArtifactsProvider>();
        _artifacts.Reducers.Returns([]);
    }

    void Because()
    {
        _first = Register();
        _unchanged = Register();
        _artifacts.Reducers.Returns([typeof(SelfReducedReadModel)]);
        _changed = Register();
    }

    [Fact] void should_reuse_the_unchanged_snapshot() => _unchanged.ShouldBeSame(_first);
    [Fact] void should_preserve_the_first_containers_snapshot() => _first.ShouldBeEmpty();
    [Fact] void should_claim_newly_discovered_self_models() => _changed.ShouldContainOnly(typeof(SelfReducedReadModel));

    IEnumerable<Type> Register()
    {
        using var provider = new ServiceCollection().AddReadModels(_artifacts).BuildServiceProvider();
        return provider.GetServices<ICanResolveReadModelForCommand>().OfType<ChronicleReadModelForCommandResolver>().Single().ReadModelTypes;
    }
}
