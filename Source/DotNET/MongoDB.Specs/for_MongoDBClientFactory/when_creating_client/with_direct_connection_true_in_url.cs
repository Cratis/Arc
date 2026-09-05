// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Castle.DynamicProxy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Cratis.Arc.MongoDB.for_MongoDBClientFactory.when_creating_client;

public class with_direct_connection_true_in_url : Specification
{
    MongoClientSettings _settings = null!;
    bool? _directConnection;

    void Because()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddLogging();
        services.AddCratisArcMeter();
        services.AddCratisMongoDB(options =>
        {
            options.Server = "mongodb://localhost:27017";
            options.Database = "test";
        });

        using var serviceProvider = services.BuildServiceProvider();
        var factory = serviceProvider.GetRequiredService<IMongoDBClientFactory>();

        _settings = MongoClientSettings.FromConnectionString("mongodb://mongodb:27017/?directConnection=true");
        var client = factory.Create(_settings);
        var target = ((IProxyTargetAccessor)client).DynProxyGetTarget() as MongoClient;

        _directConnection = target!.Settings.DirectConnection;
    }

    [Fact] void should_keep_direct_connection_from_url() => _directConnection.ShouldEqual(true);
}
