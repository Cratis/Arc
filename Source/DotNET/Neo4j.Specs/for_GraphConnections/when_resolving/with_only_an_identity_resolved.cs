// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Connections;

namespace Cratis.Arc.Neo4j.for_GraphConnections.when_resolving;

public class with_only_an_identity_resolved : given.graph_connections
{
    GraphConnection _result;
    object? _firstManager;
    object? _secondManager;

    void Establish()
    {
        _options.ConnectionResolution.Identity.UseHttp(new Uri("https://platform.example/connections"));
        _resolver.Resolve(Arg.Any<ConnectionRequest>(), Arg.Any<CancellationToken>()).Returns(new ResolvedConnection(Identity: new("acme-user", "acme-password")));
    }

    void Because()
    {
        _result = _connections.Resolve();
        _firstManager = _createdWith;
        _connections.Resolve();
        _secondManager = _createdWith;
    }

    [Fact] void should_use_the_configured_server() => _createdForServer!.Host.ShouldEqual("configured");
    [Fact] void should_use_the_database_that_is_named() => _result.DatabaseName.ShouldEqual("orders-acme");
    [Fact] void should_keep_one_driver_identity_for_the_same_tenant() => _secondManager.ShouldEqual(_firstManager);
}
