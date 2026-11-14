// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Graphs;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Neo4j.Chronicle.for_GraphViewsServiceCollectionExtensions;

public class when_adding_neo4j_without_graph_views : Specification
{
    ServiceCollection _services;

    void Because()
    {
        _services = new ServiceCollection();
        _services.AddCratisNeo4j();
    }

    [Fact] void should_not_register_a_graph() => _services.Any(_ => _.ServiceType == typeof(IGraph) || _.ServiceType == typeof(IGraphQueryExecutor) || _.ServiceType == typeof(IGraphViews)).ShouldBeFalse();
}
