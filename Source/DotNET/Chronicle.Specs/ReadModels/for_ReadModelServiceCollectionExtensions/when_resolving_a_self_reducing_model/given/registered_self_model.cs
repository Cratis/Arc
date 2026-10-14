// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Commands;
using Cratis.Arc.Commands;
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.ReadModels;
using Cratis.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.ReadModels.for_ReadModelServiceCollectionExtensions.when_resolving_a_self_reducing_model.given;

public class registered_self_model : Specification
{
    protected EventSourceId _id;
    protected IReadModels _readModels;
    protected object? _resolved;
    ServiceProvider _provider;
    IServiceScope _scope;

    void Establish()
    {
        _id = EventSourceId.New();
        _readModels = Substitute.For<IReadModels>();
        var artifacts = Substitute.For<IClientArtifactsProvider>();
        artifacts.Reducers.Returns([typeof(SelfReducedReadModel)]);
        var services = new ServiceCollection().AddReadModels(artifacts).AddSingleton(_readModels);
        var values = new CommandContextValues
        {
            { WellKnownCommandContextKeys.EventSourceId, _id }
        };
        services.AddScoped(_ => new CommandContext(CorrelationId.New(), typeof(object), new object(), [], values, null));
        _provider = services.BuildServiceProvider();
        _scope = _provider.CreateScope();
    }

    protected void Resolve() => _resolved = _scope.ServiceProvider.GetService<SelfReducedReadModel>();

    void Destroy()
    {
        _scope.Dispose();
        _provider.Dispose();
    }
}
