// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Connections;

namespace Cratis.Arc.MongoDB.for_MongoDBConnections.when_resolving;

public class without_connection_resolution : given.mongodb_connections
{
    MongoDBConnection _result;

    void Because() => _result = _connections.Resolve();

    [Fact] void should_use_the_configured_client() => _result.Client.ShouldBeSame(_client);
    [Fact] void should_use_the_database_name_resolver() => _result.DatabaseName.ShouldEqual("app+acme");
    [Fact] void should_not_resolve_anything() => _resolver.DidNotReceive().Resolve(Arg.Any<ConnectionRequest>(), Arg.Any<CancellationToken>());
}
