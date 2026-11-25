// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Connections;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.MongoDB.for_ServiceCollectionExtensions.when_adding_mongodb;

public class with_named_sinks_in_a_custom_configuration_section : Specification
{
    MongoDBOptions _options;

    void Because()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Persistence:MongoDB:Server"] = "mongodb://localhost:27017",
            ["Persistence:MongoDB:Database"] = "db",
            ["Persistence:MongoDB:Sinks:Reporting:Server"] = "mongodb://reporting:27017",
            ["Persistence:MongoDB:ConnectionResolution:Connection:Resolver"] = "Http",
            ["Persistence:MongoDB:ConnectionResolution:Connection:Http:Endpoint"] = "https://resolver.invalid/connection",
            ["Persistence:MongoDB:ConnectionResolution:Connection:Http:Headers:region"] = "west"
        }).Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging();
        services.AddCratisArcMeter();
        services.AddCratisMongoDB(mongoDBConfigSectionPath: "Persistence:MongoDB");
        using var provider = services.BuildServiceProvider();
        _options = provider.GetRequiredService<IOptions<MongoDBOptions>>().Value;
    }

    [Fact] void should_bind_the_custom_section() => _options.Database.ShouldEqual("db");
    [Fact] void should_bind_the_named_sink() => _options.Sinks["Reporting"].Server.ShouldEqual("mongodb://reporting:27017");
    [Fact] void should_bind_the_connection_resolver() => _options.ConnectionResolution.Connection.Resolver.ShouldEqual(ConnectionResolverType.Http);
    [Fact] void should_bind_the_connection_endpoint() => _options.ConnectionResolution.Connection.Http.Endpoint.ShouldEqual(new Uri("https://resolver.invalid/connection"));
    [Fact] void should_bind_the_connection_headers() => _options.ConnectionResolution.Connection.Http.Headers["region"].ShouldEqual("west");
}
