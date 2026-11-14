// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Graphs;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Neo4j.Chronicle.for_GraphViewsServiceCollectionExtensions.when_adding_graph_views;

public class with_views_registered_before : Specification
{
    ServiceCollection _services;
    IGraphViews _views;
    ServiceProvider _provider;

    void Establish()
    {
        _views = Substitute.For<IGraphViews>();
        _services = new ServiceCollection();
        _services.AddSingleton(_views);
    }

    void Because()
    {
        _services.AddCratisNeo4jGraphViews();
        _services.AddCratisNeo4jGraphViews();
        _provider = _services.BuildServiceProvider();
    }

    [Fact] void should_keep_the_views_registered_before() => _provider.GetRequiredService<IGraphViews>().ShouldBeSame(_views);
    [Fact] void should_register_the_graph_once() => _services.Count(_ => _.ServiceType == typeof(IGraph)).ShouldEqual(1);
    [Fact] void should_register_the_executor_once() => _services.Count(_ => _.ServiceType == typeof(IGraphQueryExecutor)).ShouldEqual(1);
}
