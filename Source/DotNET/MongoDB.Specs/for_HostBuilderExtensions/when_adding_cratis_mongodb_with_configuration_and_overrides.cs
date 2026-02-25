// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.MongoDB;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Hosting.for_HostBuilderExtensions;

public class when_adding_cratis_mongodb_with_configuration_and_overrides : Specification
{
    string _configuredDatabase = string.Empty;
    string _configuredServer = string.Empty;
    string _overrideDatabase = string.Empty;
    string _resolvedDatabase = string.Empty;

    void Establish()
    {
        _configuredServer = "mongodb://localhost:27017";
        _configuredDatabase = "from-config";
        _overrideDatabase = "from-override";
    }

    void Because()
    {
        var configurationPath = ConfigurationPath.Combine(ServiceCollectionExtensions.DefaultSectionPaths);
        var configurationValues = new Dictionary<string, string?>
        {
            [$"{configurationPath}:Server"] = _configuredServer,
            [$"{configurationPath}:Database"] = _configuredDatabase
        };

        using var host = new HostBuilder()
            .ConfigureAppConfiguration((_, configurationBuilder) => configurationBuilder.AddInMemoryCollection(configurationValues))
            .AddCratisMongoDB(options => options.Database = _overrideDatabase)
            .Build();

        var options = host.Services.GetRequiredService<IOptions<MongoDBOptions>>();
        _resolvedDatabase = options.Value.Database;
    }

    [Fact] void should_use_value_from_configure_options() => _resolvedDatabase.ShouldEqual(_overrideDatabase);
}
