// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Graphs;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Neo4j.Chronicle.Testing.for_GraphServiceCollectionExtensions;

public class when_using_a_graph : Specification
{
    ServiceCollection _services;
    IGraph _graph;
    ServiceProvider _provider;

    void Establish()
    {
        _graph = Substitute.For<IGraph>();
        _services = new ServiceCollection();
        _services.AddCratisNeo4jGraphViews();
    }

    void Because()
    {
        _services.UseGraph(_graph);
        _provider = _services.BuildServiceProvider();
    }

    [Fact] void should_give_the_graph() => _provider.CreateScope().ServiceProvider.GetRequiredService<IGraph>().ShouldBeSame(_graph);
    [Fact] void should_replace_the_registered_graph() => _services.Count(_ => _.ServiceType == typeof(IGraph)).ShouldEqual(1);
}
