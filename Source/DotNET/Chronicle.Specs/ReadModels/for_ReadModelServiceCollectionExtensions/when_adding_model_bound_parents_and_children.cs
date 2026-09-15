// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;
using Cratis.Chronicle;
using Cratis.Chronicle.Projections.ModelBound;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.ReadModels.for_ReadModelServiceCollectionExtensions;

public class when_adding_model_bound_parents_and_children : Specification
{
    IServiceCollection _services;
    IClientArtifactsProvider _artifacts;
    ServiceProvider _provider;

    void Establish()
    {
        _services = new ServiceCollection();
        _artifacts = Substitute.For<IClientArtifactsProvider>();
        _artifacts.Projections.Returns([]);
        _artifacts.Reducers.Returns([]);
        _artifacts.ModelBoundProjections.Returns([typeof(Parent), typeof(Child), typeof(Details), typeof(AbstractRoot), typeof(ValueRoot)]);
    }

    void Because()
    {
        _services.AddReadModels(_artifacts);
        _provider = _services.BuildServiceProvider();
    }

    void Destroy() => _provider.Dispose();

    [Fact] void should_register_the_parent_as_scoped() => _services.ShouldContain(_ => _.ServiceType == typeof(Parent) && _.Lifetime == ServiceLifetime.Scoped);
    [Fact] void should_not_register_the_child_as_a_service() => _services.ShouldNotContain(_ => _.ServiceType == typeof(Child));
    [Fact] void should_not_register_the_nested_subobject_as_a_service() => _services.ShouldNotContain(_ => _.ServiceType == typeof(Details));
    [Fact] void should_classify_only_the_concrete_parent_as_a_command_read_model() => _provider.GetRequiredService<RegisteredReadModelTypes>().Types.ShouldContainOnly(typeof(Parent));
    [Fact] void should_claim_only_the_concrete_parent_for_chronicle_resolution() => _provider.GetServices<ICanResolveReadModelForCommand>().OfType<ChronicleReadModelForCommandResolver>().Single().ReadModelTypes.ShouldContainOnly(typeof(Parent));

    record Parent([ChildrenFrom<ChildAdded>(key: nameof(ChildAdded.Id))] IEnumerable<Child> Children);
    record Child(Details Details);
    record Details(string Name);
    abstract record AbstractRoot;
    record struct ValueRoot(int Value);
    record ChildAdded(Guid Id);
}
