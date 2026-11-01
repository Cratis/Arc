// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.MongoDB.for_ServiceCollectionExtensions.when_adding_mongodb;

public class with_resilience_disabled_in_the_options_callback : Specification
{
    MongoDBOptions _options;

    void Because()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Cratis:MongoDB:Server"] = "mongodb://localhost:27017",
            ["Cratis:MongoDB:Database"] = "db",
            ["Cratis:MongoDB:EnableResilience"] = "true"
        }).Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging();
        services.AddCratisArcMeter();
        services.AddCratisMongoDB(options => options.EnableResilience = false);
        using var provider = services.BuildServiceProvider();
        _options = provider.GetRequiredService<IOptions<MongoDBOptions>>().Value;
    }

    [Fact] void should_prefer_the_callback() => _options.EnableResilience.ShouldBeFalse();
}
