// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Connections;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.MongoDB.for_ServiceCollectionExtensions.when_adding_mongodb;

public class with_connection_resolution_in_configuration : Specification
{
    MongoDBOptions _options;

    void Because()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Cratis:MongoDB:Server"] = "mongodb://configured:27017",
                ["Cratis:MongoDB:Database"] = "app",
                ["Cratis:MongoDB:ConnectionResolution:Identity:Resolver"] = "Http",
                ["Cratis:MongoDB:ConnectionResolution:Identity:Http:Endpoint"] = "https://platform.example/connections"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging();
        services.AddCratisArcMeter();
        services.AddCratisMongoDB();

        using var serviceProvider = services.BuildServiceProvider();
        _options = serviceProvider.GetRequiredService<IOptions<MongoDBOptions>>().Value;
    }

    [Fact] void should_bind_the_identity_resolver() => _options.ConnectionResolution.Identity.Resolver.ShouldEqual(ConnectionResolverType.Http);
    [Fact] void should_bind_the_endpoint() => _options.ConnectionResolution.Identity.Http.Endpoint.ShouldEqual(new Uri("https://platform.example/connections"));
    [Fact] void should_leave_the_connection_slot_as_default() => _options.ConnectionResolution.Connection.Resolver.ShouldEqual(ConnectionResolverType.Default);
}
