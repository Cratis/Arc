// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Neo4j.Chronicle.given;
using Cratis.Arc.Tenancy;
using Cratis.Chronicle;
using Cratis.Chronicle.Graphs;
using Cratis.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Neo4j.Chronicle.for_GraphViewsServiceCollectionExtensions.when_adding_graph_views;

public class with_neo4j_added : Specification
{
    ServiceCollection _services;
    ServiceProvider _provider;
    IClientArtifactsProvider _artifacts;
    IServiceScope _scope;
    IServiceScope _otherScope;

    void Establish()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Cratis:Neo4j:Server"] = "bolt://localhost:7687",
            ["Cratis:Neo4j:EventStore"] = "Organization"
        }).Build();
        _artifacts = new GraphArtifacts([typeof(Person), typeof(Group), typeof(MemberOf)], [typeof(GroupSummary)]);

        _services = new ServiceCollection();
        _services.AddSingleton<IConfiguration>(configuration);
        _services.AddLogging();
        var accessor = Substitute.For<ITenantIdAccessor>();
        accessor.Current.Returns(new TenantId("acme"));
        _services.AddSingleton(accessor);
        _services.AddSingleton(_artifacts);
        _services.AddSingleton<INamingPolicy>(new CamelCaseNamingPolicy());
        _services.AddCratisNeo4j();
    }

    void Because()
    {
        _services.AddCratisNeo4jGraphViews();
        _provider = _services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
        _scope = _provider.CreateScope();
        _otherScope = _provider.CreateScope();
    }

    [Fact] void should_give_a_queryable_graph() => _scope.ServiceProvider.GetRequiredService<IGraph>().ShouldBeOfExactType<QueryableGraph>();
    [Fact] void should_give_one_graph_per_scope() => _scope.ServiceProvider.GetRequiredService<IGraph>().ShouldNotBeSame(_otherScope.ServiceProvider.GetRequiredService<IGraph>());
    [Fact] void should_keep_the_graph_within_a_scope() => _scope.ServiceProvider.GetRequiredService<IGraph>().ShouldBeSame(_scope.ServiceProvider.GetRequiredService<IGraph>());
    [Fact] void should_execute_queries_on_neo4j() => _scope.ServiceProvider.GetRequiredService<IGraphQueryExecutor>().ShouldBeOfExactType<Neo4jGraphQueryExecutor>();
    [Fact] void should_discover_the_views_from_the_registered_client_artifacts() => _provider.GetRequiredService<IGraphViews>().IsView(typeof(GroupSummary)).ShouldBeTrue();
    [Fact] void should_keep_the_scoped_graph_session_of_neo4j() => _scope.ServiceProvider.GetRequiredService<IGraphSession>().DatabaseName.ShouldEqual("organization-acme");
}
