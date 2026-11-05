// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Options;

namespace Cratis.Arc.Neo4j.for_DefaultGraphDatabaseNameResolver.when_resolving;

public class with_a_prefix : Specification
{
    DefaultGraphDatabaseNameResolver _resolver;
    string _result;

    void Establish()
    {
        var options = Substitute.For<IOptionsMonitor<Neo4jOptions>>();
        options.CurrentValue.Returns(new Neo4jOptions { EventStore = "Orders", DatabasePrefix = "shop" });
        var namespaces = Substitute.For<IGraphNamespaceResolver>();
        namespaces.Resolve().Returns("acme");
        _resolver = new(options, namespaces);
    }

    void Because() => _result = _resolver.Resolve();

    [Fact] void should_start_with_the_prefix() => _result.ShouldEqual("shop-orders-acme");
}
