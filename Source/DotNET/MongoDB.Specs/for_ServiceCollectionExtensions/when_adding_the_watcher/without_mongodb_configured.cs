// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.MongoDB.for_ServiceCollectionExtensions.when_adding_the_watcher;

public class without_mongodb_configured : Specification
{
    ServiceProvider _serviceProvider;
    IMongoDBWatcher _first;
    IMongoDBWatcher _second;

    void Because()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMongoDBWatcher();
        _serviceProvider = services.BuildServiceProvider();
        _first = _serviceProvider.GetRequiredService<IMongoDBWatcher>();
        _second = _serviceProvider.GetRequiredService<IMongoDBWatcher>();
    }

    void Destroy() => _serviceProvider.Dispose();

    [Fact] void should_resolve_the_watcher() => _first.ShouldBeOfExactType<MongoDBWatcher>();
    [Fact] void should_resolve_one_watcher_for_the_process() => _first.ShouldBeSame(_second);
}
