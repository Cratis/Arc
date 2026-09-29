// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.MongoDB.for_MongoDBClientFactory.given;

public class a_client_factory : Specification
{
    protected ServiceProvider _serviceProvider;
    protected IMongoDBClientFactory _factory;

    protected virtual void ConfigureOptions(MongoDBOptions options)
    {
    }

    void Establish()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddLogging();
        services.AddCratisArcMeter();
        services.AddCratisMongoDB(options =>
        {
            options.Server = "mongodb://localhost:27017";
            options.Database = "test";
            ConfigureOptions(options);
        });

        _serviceProvider = services.BuildServiceProvider();
        _factory = _serviceProvider.GetRequiredService<IMongoDBClientFactory>();
    }

    void Destroy() => _serviceProvider.Dispose();
}
