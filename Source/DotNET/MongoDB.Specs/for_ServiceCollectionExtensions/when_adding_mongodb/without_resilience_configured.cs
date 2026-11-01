// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.MongoDB.for_ServiceCollectionExtensions.when_adding_mongodb;

public class without_resilience_configured : Specification
{
    MongoDBOptions _options;

    void Because()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Cratis:MongoDB:Server"] = "mongodb://localhost:27017",
            ["Cratis:MongoDB:Database"] = "db"
        }).Build());
        services.AddLogging();
        services.AddCratisArcMeter();
        services.AddCratisMongoDB();
        using var provider = services.BuildServiceProvider();
        _options = provider.GetRequiredService<IOptions<MongoDBOptions>>().Value;
    }

    [Fact] void should_enable_resilience() => _options.EnableResilience.ShouldBeTrue();
}
