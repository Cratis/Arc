// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Commands;
using Cratis.Arc.Commands;
using Cratis.Arc.MongoDB;
using Cratis.Arc.Queries;
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.ReadModels;
using Cratis.Execution;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Cratis.Arc.Chronicle.ReadModels.for_ReadModelServiceCollectionExtensions.when_injecting_with_both_providers.given;

public class a_read_model_with_both_providers<TReadModel> : Specification
    where TReadModel : class
{
    protected EventSourceId _eventSourceId;
    protected IReadModels _readModels;
    protected TReadModel _stored;
    protected TReadModel _released;
    protected IMongoCollection<TReadModel> _mongoCollection;
    protected int _mongoCollectionResolutions;
    protected MongoDBReadModelForCommandResolver _mongoDBResolver;
    protected ChronicleReadModelForCommandResolver _chronicleResolver;
    protected CommandHandlerArgumentResolver _argumentResolver;
    protected ICommandHandler _handler;
    protected CommandContext _commandContext;
    protected IServiceScope _scope;
    protected CommandHandlerArgumentResolution _result;
    ServiceProvider _provider;

    void Establish()
    {
        _eventSourceId = EventSourceId.New();
        _readModels = Substitute.For<IReadModels>();
        _mongoCollection = Substitute.For<IMongoCollection<TReadModel>>();
        _argumentResolver = new(new CommandProvideInvoker());
        _handler = Substitute.For<ICommandHandler>();
        _handler.Parameters.Returns(typeof(ReadModelCommand).GetMethod(nameof(ReadModelCommand.Handle))!.GetParameters());
        var values = new CommandContextValues
        {
            { WellKnownCommandContextKeys.EventSourceId, _eventSourceId },
            { CommandContextKeys.ResolvedKey, _eventSourceId.Value }
        };
        _commandContext = new(CorrelationId.New(), typeof(ReadModelCommand), new ReadModelCommand(), [], values);
    }

    protected void Configure(TReadModel stored, TReadModel released, bool mongoDBFirst)
    {
        _stored = stored;
        _released = released;
        _readModels.GetInstanceById(typeof(TReadModel), _eventSourceId).Returns(Task.FromResult<object>(_stored));
        _readModels.Release(_stored).Returns(Task.FromResult(_released));
        var artifacts = Substitute.For<IClientArtifactsProvider>();
        artifacts.Projections.Returns([typeof(ProjectionForOwnedReadModel)]);
        artifacts.ModelBoundProjections.Returns([]);
        artifacts.Reducers.Returns([typeof(ReducerForOwnedReadModel), typeof(SelfReducingReadModel)]);
        var services = new ServiceCollection();

        // AddReadModels is the registration invoked by WithChronicle. Use that seam without activating a
        // network client; both providers' discovery, resolver registration and ownership selection stay real.
        if (mongoDBFirst)
        {
            services.AddCratisMongoDB();
            services.AddReadModels(artifacts);
        }
        else
        {
            services.AddReadModels(artifacts);
            services.AddCratisMongoDB();
        }

        services.AddScoped(_ => _readModels);
        services.AddScoped(_ => _commandContext);

        // A closed collection registration avoids any MongoDB connection and observes even an attempted fallback.
        services.AddScoped(_ =>
        {
            _mongoCollectionResolutions++;
            return _mongoCollection;
        });
        _provider = services.BuildServiceProvider();
        _scope = _provider.CreateScope();
        var resolvers = _scope.ServiceProvider.GetServices<ICanResolveReadModelForCommand>().ToArray();
        _mongoDBResolver = resolvers.OfType<MongoDBReadModelForCommandResolver>().Single();
        _chronicleResolver = resolvers.OfType<ChronicleReadModelForCommandResolver>().Single();
    }

    protected void ShouldHaveLoadedFromChronicle() =>
        _readModels.ReceivedCalls().Count(call =>
            call.GetMethodInfo().Name == nameof(IReadModels.GetInstanceById) &&
            call.GetArguments() is [Type modelType, ReadModelKey key, _] &&
            modelType == typeof(TReadModel) && key.Value == _eventSourceId.Value).ShouldEqual(1);

    void Destroy()
    {
        _scope?.Dispose();
        _provider?.Dispose();
    }

    protected record ReadModelCommand
    {
        public void Handle(TReadModel? readModel) { }
    }
}
