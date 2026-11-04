// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Metrics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.MongoDB.for_MongoDBClientFactory.when_creating_client;

public class and_the_metrics_are_additive : Specification
{
    readonly List<string> _legacyNames = [];
    readonly List<string> _alignedNames = [];

    void Because()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddLogging();
        services.AddCratisArcMeter();
        services.AddCratisMongoDB(options =>
        {
            options.Server = "mongodb://localhost:27017";
            options.Database = "metrics";
        });
        using var provider = services.BuildServiceProvider();
        var legacy = provider.GetRequiredKeyedService<Meter>(WellKnownDiagnostics.MeterName);
        var aligned = provider.GetRequiredKeyedService<Meter>(WellKnownMongoDBDiagnostics.MeterName);
        using var listener = new MeterListener
        {
            InstrumentPublished = (instrument, _) =>
            {
                if (ReferenceEquals(instrument.Meter, legacy)) _legacyNames.Add(instrument.Name);
                if (ReferenceEquals(instrument.Meter, aligned)) _alignedNames.Add(instrument.Name);
            }
        };
        listener.Start();
        using var client = provider.GetRequiredService<IMongoDBClientFactory>().Create();
    }

    [Fact] void should_still_initialize_legacy_gauges() => _legacyNames.ShouldContain("mongodb-open-connections");
    [Fact] void should_initialize_distinct_dotted_gauges() => _alignedNames.ShouldContainOnly(["mongodb.open.connections", "mongodb.checked.out.connections", "mongodb.connections.in.pool", "mongodb.commands"]);
}
