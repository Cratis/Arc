// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Connections;

namespace Cratis.Arc.Neo4j.for_GraphConnections.when_resolving;

public class with_a_resolved_database_and_identity : given.graph_connections
{
    ConnectionRequest? _request;
    GraphConnection _result;

    void Establish()
    {
        _options.ConnectionResolution.Identity.UseHttp(new Uri("https://platform.example/connections"));
        _resolver.Resolve(Arg.Do<ConnectionRequest>(_ => _request = _), Arg.Any<CancellationToken>())
            .Returns(new ResolvedConnection("bolt://acme.graph.example:7687", "acme-graph", Identity: new("acme-user", "acme-password")));
    }

    void Because() => _result = _connections.Resolve();

    [Fact] void should_resolve_for_neo4j() => _request!.Storage.ShouldEqual(WellKnownConnectionStorage.Neo4j);
    [Fact] void should_resolve_for_runtime() => _request!.Purpose.ShouldEqual(ConnectionPurpose.Runtime);
    [Fact] void should_resolve_for_the_current_tenant() => _request!.Scope[WellKnownConnectionScopeKeys.Tenant].ShouldEqual("acme");
    [Fact] void should_use_the_resolved_database() => _result.DatabaseName.ShouldEqual("acme-graph");
    [Fact] void should_use_the_resolved_server() => _createdForServer!.Host.ShouldEqual("acme.graph.example");
    [Fact] void should_follow_the_resolution_for_the_identity() => _createdWith.ShouldBeOfExactType<Neo4jConnectionResolutionHooks>();
}
