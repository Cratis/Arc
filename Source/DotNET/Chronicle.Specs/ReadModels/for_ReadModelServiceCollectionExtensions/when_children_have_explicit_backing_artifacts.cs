// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;
using Cratis.Chronicle;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.Reducers;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.ReadModels.for_ReadModelServiceCollectionExtensions;

public class when_children_have_explicit_backing_artifacts : Specification
{
    IServiceCollection _services;
    IClientArtifactsProvider _artifacts;
    ServiceProvider _provider;

    void Establish()
    {
        _services = new ServiceCollection();
        _artifacts = Substitute.For<IClientArtifactsProvider>();
        _artifacts.Projections.Returns([typeof(ChildProjection)]);
        _artifacts.Reducers.Returns([typeof(ChildReducer)]);
        _artifacts.ModelBoundProjections.Returns([typeof(Parent), typeof(ProjectedChild), typeof(ReducedChild), typeof(EmbeddedChild)]);
    }

    void Because()
    {
        _services.AddReadModels(_artifacts);
        _provider = _services.BuildServiceProvider();
    }

    void Destroy() => _provider.Dispose();

    [Fact] void should_register_the_explicit_projection_target_as_scoped() => _services.ShouldContain(_ => _.ServiceType == typeof(ProjectedChild) && _.Lifetime == ServiceLifetime.Scoped);
    [Fact] void should_register_the_explicit_reducer_target_as_scoped() => _services.ShouldContain(_ => _.ServiceType == typeof(ReducedChild) && _.Lifetime == ServiceLifetime.Scoped);
    [Fact] void should_not_register_a_child_without_explicit_backing() => _services.ShouldNotContain(_ => _.ServiceType == typeof(EmbeddedChild));
    [Fact] void should_include_explicit_targets_in_the_registered_read_model_types() => _provider.GetRequiredService<RegisteredReadModelTypes>().Types.ShouldContainOnly(typeof(Parent), typeof(ProjectedChild), typeof(ReducedChild));
    [Fact] void should_include_explicit_targets_in_chronicle_resolution() => _provider.GetServices<ICanResolveReadModelForCommand>().OfType<ChronicleReadModelForCommandResolver>().Single().ReadModelTypes.ShouldContainOnly(typeof(Parent), typeof(ProjectedChild), typeof(ReducedChild));

    record Parent(
        [ChildrenFrom<ChildAdded>(key: nameof(ChildAdded.Id))] IEnumerable<ProjectedChild> ProjectedChildren,
        [ChildrenFrom<ChildAdded>(key: nameof(ChildAdded.Id))] IEnumerable<ReducedChild> ReducedChildren,
        [ChildrenFrom<ChildAdded>(key: nameof(ChildAdded.Id))] IEnumerable<EmbeddedChild> EmbeddedChildren);

    record ProjectedChild(string Name);
    record ReducedChild(string Name);
    record EmbeddedChild(string Name);
    record ChildAdded(Guid Id);

    class ChildProjection : IProjectionFor<ProjectedChild>
    {
        public void Define(IProjectionBuilderFor<ProjectedChild> builder)
        {
        }
    }

    class ChildReducer : IReducerFor<ReducedChild>;
}
