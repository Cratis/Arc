// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Arc.Chronicle.ReadModels.for_ReadModelDependencyResolver.given;

public class a_read_model_dependency_resolver : Specification
{
    protected IClientArtifactsProvider _clientArtifactsProvider;
    protected IReadModels _readModels;
    protected IServiceProvider _serviceProvider;
    protected ReadModelDependencyResolver _resolver;

    void Establish()
    {
        _clientArtifactsProvider = Substitute.For<IClientArtifactsProvider>();
        _readModels = Substitute.For<IReadModels>();
        _serviceProvider = Substitute.For<IServiceProvider>();
        _serviceProvider.GetService(typeof(IReadModels)).Returns(_readModels);

        _clientArtifactsProvider.Projections.Returns([typeof(TestProjection)]);
        _clientArtifactsProvider.ModelBoundProjections.Returns([typeof(ModelBoundReadModel)]);

        _resolver = new ReadModelDependencyResolver(_clientArtifactsProvider);
    }

    protected static CommandContextValues CreateValuesWithEventSourceId(EventSourceId eventSourceId) =>
        new() { { Commands.WellKnownCommandContextKeys.EventSourceId, eventSourceId } };

    protected class TestReadModel
    {
        public string Name { get; set; } = string.Empty;
    }

    protected class TestProjection : IProjectionFor<TestReadModel>
    {
        public void Define(IProjectionBuilderFor<TestReadModel> builder)
        {
        }
    }

    protected class ModelBoundReadModel
    {
        public string Value { get; set; } = string.Empty;
    }
}
