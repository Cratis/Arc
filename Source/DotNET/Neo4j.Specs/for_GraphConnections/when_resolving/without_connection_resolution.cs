// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Neo4j.for_GraphConnections.when_resolving;

public class without_connection_resolution : given.graph_connections
{
    GraphConnection _result;

    void Because() => _result = _connections.Resolve();

    [Fact] void should_use_the_configured_server() => _createdForServer!.Host.ShouldEqual("configured");
    [Fact] void should_use_the_configured_credentials() => _createdWith.ShouldEqual(new BasicGraphAuthentication("neo4j", "configured-password"));
    [Fact] void should_use_the_named_database() => _result.DatabaseName.ShouldEqual("orders-acme");
    [Fact] void should_use_the_driver() => _result.Driver.ShouldEqual(_driver);
    [Fact] void should_not_ask_the_resolver() => _resolver.DidNotReceive().Resolve(Arg.Any<Cratis.Arc.Connections.ConnectionRequest>(), Arg.Any<CancellationToken>());
}
