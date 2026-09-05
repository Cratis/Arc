// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Commands;
using Cratis.Arc.Commands;
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.ReadModels;
using Cratis.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.ReadModels.for_ReadModelServiceCollectionExtensions.given;

public class nullable_read_model_injection : Specification
{
    protected EventSourceId _eventSourceId;
    protected ServiceProvider _rootProvider;
    protected IServiceScope _scope;
    protected IServiceProvider _serviceProvider;
    protected IReadModels _readModels;

    void Establish()
    {
        _eventSourceId = EventSourceId.New();
        _readModels = Substitute.For<IReadModels>();
        _readModels.GetInstanceById(typeof(TestReadModel), _eventSourceId, default).Returns(Task.FromResult<object>(null!));

        var clientArtifactsProvider = Substitute.For<IClientArtifactsProvider>();
        clientArtifactsProvider.Projections.Returns([]);
        clientArtifactsProvider.ModelBoundProjections.Returns([typeof(TestReadModel)]);

        var commandContextValues = new CommandContextValues
        {
            [WellKnownCommandContextKeys.EventSourceId] = _eventSourceId
        };
        var commandContext = new CommandContext(CorrelationId.New(), typeof(TestCommand), new TestCommand(), [], commandContextValues);

        _rootProvider = new ServiceCollection()
            .AddScoped(_ => commandContext)
            .AddScoped(_ => _readModels)
            .AddReadModels(clientArtifactsProvider)
            .BuildServiceProvider();
        _scope = _rootProvider.CreateScope();
        _serviceProvider = _scope.ServiceProvider;
    }

    void Destroy()
    {
        _scope.Dispose();
        _rootProvider.Dispose();
    }

    protected record TestCommand;
    protected record TestReadModel(string Value);
}
