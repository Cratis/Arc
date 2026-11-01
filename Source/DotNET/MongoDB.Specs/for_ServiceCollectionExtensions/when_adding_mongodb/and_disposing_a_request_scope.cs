// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Castle.DynamicProxy;
using Cratis.Arc.Connections;
using Cratis.Arc.Tenancy;
using Cratis.Metrics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Cratis.Arc.MongoDB.for_ServiceCollectionExtensions.when_adding_mongodb;

public class and_disposing_a_request_scope : for_MongoDBClientFactory.given.a_factory_over_a_native_client
{
    IMongoClient _first;
    IMongoClient _second;
    long _result;
    Type _connectionsType;

    async Task Because()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddLogging();
        services.AddCratisArcMeter();
        services.AddCratisMongoDB(options =>
        {
            options.Server = _options.Server;
            options.Database = _options.Database;
        });
        services.AddSingleton<IMongoDBClientFactory>(sp => new MongoDBClientFactory(
            sp.GetRequiredService<IMongoServerResolver>(),
            sp.GetRequiredKeyedService<IMeter<IMongoClient>>(Internals.MeterName),
            sp.GetRequiredService<IOptions<MongoDBOptions>>(),
            sp.GetRequiredService<ILogger<MongoDBClientFactory>>(),
            _ => _native));
        var resolvers = Substitute.For<IConnectionResolvers>();
        resolvers.For(Arg.Any<ConnectionResolutionOptions>()).Returns((ICachingConnectionResolver?)null);
        services.AddSingleton(resolvers);
        services.AddSingleton(Substitute.For<ITenantIdAccessor>());
        var databaseNameResolver = Substitute.For<IMongoDatabaseNameResolver>();
        databaseNameResolver.Resolve().Returns("db");
        services.AddSingleton(databaseNameResolver);
        _nativeCollection.CountDocumentsAsync(Arg.Any<FilterDefinition<for_MongoDBClientFactory.given.Document>>(), Arg.Any<CountOptions>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(42L));
        await using var provider = services.BuildServiceProvider();
        _connectionsType = provider.GetRequiredService<IMongoDBConnections>().GetType();
        await using (var scope = provider.CreateAsyncScope())
        {
            _first = scope.ServiceProvider.GetRequiredService<IMongoClient>();
        }
        await using (var scope = provider.CreateAsyncScope())
        {
            _second = scope.ServiceProvider.GetRequiredService<IMongoClient>();
            var database = scope.ServiceProvider.GetRequiredService<IMongoDatabase>();
            _result = await database.GetCollection<for_MongoDBClientFactory.given.Document>("docs").CountDocumentsAsync(FilterDefinition<for_MongoDBClientFactory.given.Document>.Empty);
        }
    }

    [Fact] void should_use_the_default_proxy() => ((IProxyTargetAccessor)_second).DynProxyGetTarget().ShouldBeSame(_native);
    [Fact] void should_use_the_real_connections() => _connectionsType.ShouldEqual(typeof(MongoDBConnections));
    [Fact] void should_reuse_the_cached_root_client() => _second.ShouldBeSame(_first);
    [Fact] void should_not_dispose_the_native_client() => _native.DidNotReceive().Dispose();
    [Fact] void should_allow_the_second_scope_to_use_the_collection() => _result.ShouldEqual(42L);
    [Fact] void should_reach_the_native_collection() => _nativeCollection.Received(1).CountDocumentsAsync(Arg.Any<FilterDefinition<for_MongoDBClientFactory.given.Document>>(), Arg.Any<CountOptions>(), Arg.Any<CancellationToken>());
}
