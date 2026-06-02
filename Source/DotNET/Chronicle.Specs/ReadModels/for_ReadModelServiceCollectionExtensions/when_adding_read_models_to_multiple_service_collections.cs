// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;
using Cratis.Chronicle;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.ReadModels.for_ReadModelServiceCollectionExtensions;

public class when_adding_read_models_to_multiple_service_collections : Specification
{
    IServiceCollection _firstServices;
    IServiceCollection _secondServices;

    void Establish()
    {
        var clientArtifactsProvider = Substitute.For<IClientArtifactsProvider>();
        clientArtifactsProvider.Projections.Returns([]);
        clientArtifactsProvider.ModelBoundProjections.Returns([]);

        _firstServices = new ServiceCollection();
        _secondServices = new ServiceCollection();

        _firstServices.AddReadModels(clientArtifactsProvider);
        _secondServices.AddReadModels(clientArtifactsProvider);
    }

    [Fact] void should_register_read_model_interceptor_in_first_service_collection() =>
        _firstServices.ShouldContain(ReadModelInterceptorRegistration);

    [Fact] void should_register_read_model_interceptor_in_second_service_collection() =>
        _secondServices.ShouldContain(ReadModelInterceptorRegistration);

    static bool ReadModelInterceptorRegistration(ServiceDescriptor descriptor) =>
        descriptor.ServiceType == typeof(IInterceptReadModel<>) &&
        descriptor.ImplementationType == typeof(ReadModelInterceptor<>);
}
