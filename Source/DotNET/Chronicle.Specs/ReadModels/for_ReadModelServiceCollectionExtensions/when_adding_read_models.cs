// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;
using Cratis.Chronicle;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.ReadModels.for_ReadModelServiceCollectionExtensions;

public class when_adding_read_models : Specification
{
    IServiceCollection _services;

    void Establish()
    {
        var clientArtifactsProvider = Substitute.For<IClientArtifactsProvider>();
        clientArtifactsProvider.Projections.Returns([typeof(ProjectionForReadModel)]);
        clientArtifactsProvider.ModelBoundProjections.Returns([typeof(ModelBoundReadModel)]);
        clientArtifactsProvider.Reducers.Returns([typeof(ReducerForReadModel)]);

        _services = new ServiceCollection();
        _services.AddReadModels(clientArtifactsProvider);
    }

    [Fact] void should_register_read_model_interceptor() =>
        _services.ShouldContain(_ =>
            _.ServiceType == typeof(IInterceptReadModel<>) &&
            _.ImplementationType == typeof(ReadModelInterceptor<>));

    [Fact] void should_register_scoped_resolver_for_projection_read_model() =>
        _services.ShouldContain(_ => _.ServiceType == typeof(ProjectionReadModel) && _.Lifetime == ServiceLifetime.Scoped);

    [Fact] void should_register_scoped_resolver_for_model_bound_read_model() =>
        _services.ShouldContain(_ => _.ServiceType == typeof(ModelBoundReadModel) && _.Lifetime == ServiceLifetime.Scoped);

    [Fact] void should_register_scoped_resolver_for_reducer_read_model() =>
        _services.ShouldContain(_ => _.ServiceType == typeof(ReducerReadModel) && _.Lifetime == ServiceLifetime.Scoped);
}
