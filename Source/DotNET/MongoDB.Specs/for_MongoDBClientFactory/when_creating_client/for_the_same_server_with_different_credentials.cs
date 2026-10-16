// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Cratis.Arc.MongoDB.for_MongoDBClientFactory.when_creating_client;

public class for_the_same_server_with_different_credentials : Specification
{
    IMongoClient _first;
    IMongoClient _second;
    IMongoClient _sameAsFirst;

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

        _first = factory.Create(WithCredential("tenant-a", "password-a"));
        _second = factory.Create(WithCredential("tenant-b", "password-b"));
        _sameAsFirst = factory.Create(WithCredential("tenant-a", "password-a"));
    }

    [Fact] void should_not_share_a_client_between_credentials() => _first.ShouldNotBeSame(_second);
    [Fact] void should_share_a_client_for_the_same_credential() => _first.ShouldBeSame(_sameAsFirst);

    static MongoClientSettings WithCredential(string userName, string password)
    {
        var settings = MongoClientSettings.FromConnectionString("mongodb://shared-server:27017");
        settings.Credential = MongoCredential.CreateCredential("admin", userName, password);
        return settings;
    }
}
