// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;
using Cratis.Chronicle;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.ReadModels.for_ReadModelServiceCollectionExtensions;

public class when_adding_self_reducing_models : Specification
{
    IServiceCollection _services;
    ChronicleReadModelForCommandResolver _resolver;

    void Establish()
    {
        var artifacts = Substitute.For<IClientArtifactsProvider>();
        artifacts.Reducers.Returns([typeof(SelfReducedReadModel), typeof(NominatedReducer)]);
        _services = new ServiceCollection();
        _services.AddReadModels(artifacts);
    }

    void Because()
    {
        using var provider = _services.BuildServiceProvider();
        _resolver = provider.GetServices<ICanResolveReadModelForCommand>().OfType<ChronicleReadModelForCommandResolver>().Single();
    }

    [Fact] void should_claim_only_the_self_model() => _resolver.ReadModelTypes.ShouldContainOnly(typeof(SelfReducedReadModel));
    [Fact] void should_claim_declared_ownership() => _resolver.Ownership.ShouldEqual(ReadModelForCommandOwnership.Declared);
    [Fact] void should_register_a_scoped_self_model() => _services.Single(_ => _.ServiceType == typeof(SelfReducedReadModel)).Lifetime.ShouldEqual(ServiceLifetime.Scoped);
    [Fact] void should_not_register_the_nominated_plain_reducer_as_a_model() => _services.Any(_ => _.ServiceType == typeof(NominatedReducer)).ShouldBeFalse();

    class NominatedReducer
    {
        public Task<SelfReducedReadModel> Add(SelfReducedReadModel.AmountAdded @event, SelfReducedReadModel? current) => Task.FromResult(new SelfReducedReadModel(@event.Amount));
    }
}
