// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Commands;
using Cratis.Arc.Commands;
using Cratis.Arc.Queries;
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.ReadModels;
using Cratis.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.ReadModels.for_ReadModelServiceCollectionExtensions;

public class when_a_self_model_has_a_fallback_owner
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void should_prefer_chronicle_in_either_registration_order(bool fallbackFirst)
    {
        // MongoDB claims Fallback ownership. Exercise that provider-neutral boundary without adding a MongoDB
        // dependency to Chronicle's integration package or specs.
        var fallback = Substitute.For<ICanResolveReadModelForCommand>();
        fallback.ReadModelTypes.Returns([typeof(SelfReducedReadModel)]);
        fallback.Ownership.Returns(ReadModelForCommandOwnership.Fallback);
        var artifacts = Substitute.For<IClientArtifactsProvider>();
        artifacts.Reducers.Returns([typeof(SelfReducedReadModel)]);
        var readModels = Substitute.For<IReadModels>();
        var id = EventSourceId.New();
        var stored = new SelfReducedReadModel(1);
        var released = new SelfReducedReadModel(2);
        readModels.GetInstanceById(typeof(SelfReducedReadModel), id).Returns(Task.FromResult<object>(stored));
        readModels.Release(stored).Returns(Task.FromResult(released));
        var services = new ServiceCollection().AddSingleton(readModels);
        if (fallbackFirst)
        {
            services.AddReadModelsForCommand(fallback);
            services.AddReadModels(artifacts);
        }
        else
        {
            services.AddReadModels(artifacts);
            services.AddReadModelsForCommand(fallback);
        }

        var values = new CommandContextValues { { WellKnownCommandContextKeys.EventSourceId, id } };
        services.AddScoped(_ => new CommandContext(CorrelationId.New(), typeof(object), new object(), [], values, null));
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<SelfReducedReadModel>().ShouldBeSame(released);
        fallback.DidNotReceive().Resolve(Arg.Any<Type>(), Arg.Any<CommandContext>());
    }
}
