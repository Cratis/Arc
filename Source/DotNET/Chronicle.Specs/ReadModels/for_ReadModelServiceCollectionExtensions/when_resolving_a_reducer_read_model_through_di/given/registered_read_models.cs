// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Commands;
using Cratis.Arc.Commands;
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.ReadModels;
using Cratis.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.ReadModels.for_ReadModelServiceCollectionExtensions.when_resolving_a_reducer_read_model_through_di.given;

public class registered_read_models : Specification
{
    protected EventSourceId _eventSourceId;
    protected IReadModels _readModels;
    protected ServiceProvider _serviceProvider;
    protected IServiceScope _scope;
    protected object? _resolved;

    void Establish()
    {
        _eventSourceId = EventSourceId.New();
        _readModels = Substitute.For<IReadModels>();

        var clientArtifactsProvider = Substitute.For<IClientArtifactsProvider>();
        clientArtifactsProvider.Projections.Returns([]);
        clientArtifactsProvider.ModelBoundProjections.Returns([]);
        clientArtifactsProvider.Reducers.Returns([typeof(ReducerForReadModel)]);

        var services = new ServiceCollection();
        services.AddReadModels(clientArtifactsProvider);
        services.AddSingleton(_readModels);
        services.AddScoped(_ => CreateCommandContext(_eventSourceId));

        _serviceProvider = services.BuildServiceProvider();
        _scope = _serviceProvider.CreateScope();
    }

    void Destroy()
    {
        _scope?.Dispose();
        _serviceProvider?.Dispose();
    }

    protected void ResolveReducerReadModel() =>
        _resolved = _scope.ServiceProvider.GetService(typeof(ReducerReadModel));

    static CommandContext CreateCommandContext(EventSourceId eventSourceId)
    {
        var commandContextValues = new CommandContextValues
        {
            { WellKnownCommandContextKeys.EventSourceId, eventSourceId }
        };

        return new CommandContext(CorrelationId.New(), typeof(TestCommand), new TestCommand(), [], commandContextValues, null);
    }

    protected record TestCommand;
}
