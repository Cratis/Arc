// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Connections;
using Cratis.Arc.Tenancy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Cratis.Arc.MongoDB.for_ServiceCollectionExtensions.when_adding_mongodb;

public class with_a_named_sink : Specification
{
    IMongoClient _defaultClient;
    IMongoDatabase _defaultDatabase;
    IMongoClient _sinkClient;
    IMongoDatabase _sinkDatabase;
    IMongoDBClientFactory _clientFactory;
    ServiceProvider _provider;

    void Establish()
    {
        _defaultClient = Substitute.For<IMongoClient>();
        _defaultDatabase = Substitute.For<IMongoDatabase>();
        _defaultClient.GetDatabase("db+acme", Arg.Any<MongoDatabaseSettings>()).Returns(_defaultDatabase);
        _sinkClient = Substitute.For<IMongoClient>();
        _sinkDatabase = Substitute.For<IMongoDatabase>();
        _sinkClient.GetDatabase("db+acme", Arg.Any<MongoDatabaseSettings>()).Returns(_sinkDatabase);
        _clientFactory = Substitute.For<IMongoDBClientFactory>();
        _clientFactory.Create().Returns(_defaultClient);
        _clientFactory.Create(Arg.Is<MongoUrl>(_ => _.Server.Host == "reporting")).Returns(_sinkClient);

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddLogging();
        services.AddCratisMongoDB(options =>
        {
            options.Server = "mongodb://default:27017";
            options.Database = "db";
            options.Sinks["Reporting"] = new() { Server = "mongodb://reporting:27017" };
        });
        services.AddSingleton(_clientFactory);
        var resolvers = Substitute.For<IConnectionResolvers>();
        resolvers.For(Arg.Any<ConnectionResolutionOptions>()).Returns((ICachingConnectionResolver?)null);
        services.AddSingleton(resolvers);
        services.AddSingleton(Substitute.For<ITenantIdAccessor>());
        var databaseNameResolver = Substitute.For<IMongoDatabaseNameResolver>();
        databaseNameResolver.Resolve().Returns("db+acme");
        services.AddSingleton(databaseNameResolver);
        _provider = services.BuildServiceProvider();
    }

    void Because()
    {
        using var scope = _provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<IMongoCollection<ReportInNamedSink>>();
        scope.ServiceProvider.GetRequiredService<IMongoCollection<ReportInDefaultSink>>();
    }

    void Destroy() => _provider.Dispose();

    [Fact] void should_read_the_named_sink_read_model_from_the_scope_database_on_the_sink_server() =>
        _sinkDatabase.Received(1).GetCollection<ReportInNamedSink>(Arg.Any<string>(), Arg.Any<MongoCollectionSettings>());

    [Fact] void should_read_the_other_read_model_from_the_default_database() =>
        _defaultDatabase.Received(1).GetCollection<ReportInDefaultSink>(Arg.Any<string>(), Arg.Any<MongoCollectionSettings>());

    [Fact] void should_not_read_the_named_sink_read_model_from_the_default_database() =>
        _defaultDatabase.DidNotReceive().GetCollection<ReportInNamedSink>(Arg.Any<string>(), Arg.Any<MongoCollectionSettings>());
}
