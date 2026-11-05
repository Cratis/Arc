// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Tenancy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Neo4j.Driver;

namespace Cratis.Arc.Neo4j.for_ServiceCollectionExtensions;

public class when_adding_cratis_neo4j : Specification
{
    ServiceProvider _provider;
    Neo4jOptions _options;
    IGraphDatabaseNameResolver _resolver;
    INeo4jDriverFactory _factory;
    INeo4jDriverFactory _factoryAgain;

    void Establish()
    {
        var path = ConfigurationPath.Combine(ServiceCollectionExtensions.DefaultSectionPaths);
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"{path}:Server"] = "bolt://localhost:7687",
            [$"{path}:EventStore"] = "from-config"
        }).Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging();
        var accessor = Substitute.For<ITenantIdAccessor>();
        accessor.Current.Returns(new TenantId("acme"));
        services.AddSingleton(accessor);
        services.AddCratisNeo4j(options => options.EventStore = "Orders");
        _provider = services.BuildServiceProvider();
    }

    void Because()
    {
        _options = _provider.GetRequiredService<IOptions<Neo4jOptions>>().Value;
        _resolver = _provider.GetRequiredService<IGraphDatabaseNameResolver>();
        _factory = _provider.GetRequiredService<INeo4jDriverFactory>();
        _factoryAgain = _provider.GetRequiredService<INeo4jDriverFactory>();
    }

    [Fact] void should_bind_the_options_from_the_default_section() => _options.Server.ShouldEqual("bolt://localhost:7687");
    [Fact] void should_let_the_callback_override_the_configuration() => _options.EventStore.ShouldEqual("Orders");
    [Fact] void should_default_to_a_database_per_namespace() => _options.DatabaseMode.ShouldEqual(GraphDatabaseMode.PerNamespace);
    [Fact] void should_register_the_default_database_name_resolver() => _resolver.ShouldBeOfExactType<DefaultGraphDatabaseNameResolver>();
    [Fact] void should_keep_one_driver_factory() => _factoryAgain.ShouldEqual(_factory);
    [Fact] void should_register_the_scoped_session_for_the_tenant_database() => _provider.CreateScope().ServiceProvider.GetRequiredService<IGraphSession>().DatabaseName.ShouldEqual("orders-acme");
    [Fact] void should_open_the_session_for_reading() => _provider.CreateScope().ServiceProvider.GetRequiredService<IGraphSession>().AccessMode.ShouldEqual(AccessMode.Read);
}
