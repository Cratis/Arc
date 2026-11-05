// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Options;

namespace Cratis.Arc.Neo4j.for_DefaultGraphDatabaseNameResolver.when_resolving;

public class in_single_database_mode : Specification
{
    DefaultGraphDatabaseNameResolver _resolver;
    string _result;

    void Establish()
    {
        var options = Substitute.For<IOptionsMonitor<Neo4jOptions>>();
        options.CurrentValue.Returns(new Neo4jOptions { EventStore = "Orders", DatabaseMode = GraphDatabaseMode.Single });
        var namespaces = Substitute.For<IGraphNamespaceResolver>();
        namespaces.Resolve().Returns("acme");
        _resolver = new(options, namespaces);
    }

    void Because() => _result = _resolver.Resolve();

    [Fact] void should_use_the_single_database() => _result.ShouldEqual(Neo4jOptions.DefaultSingleDatabase);
}
