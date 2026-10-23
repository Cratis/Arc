// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.MongoDB.for_ServiceCollectionExtensions.when_adding_the_watcher;

public class and_mongodb_is_configured_too : Specification
{
    IServiceCollection _services;

    void Because()
    {
        _services = new ServiceCollection();
        _services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        _services.AddLogging();
        _services.AddCratisArcMeter();
        _services.AddMongoDBWatcher();
        _services.AddCratisMongoDB();
    }

    [Fact] void should_register_the_watcher_once() => _services.Count(_ => _.ServiceType == typeof(IMongoDBWatcher)).ShouldEqual(1);
}
