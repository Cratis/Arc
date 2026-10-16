// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Connections;

namespace Cratis.Arc.MongoDB.for_MongoDBConnections.when_resolving;

public class with_a_resolved_database_and_identity : given.mongodb_connections
{
    ConnectionRequest? _request;
    MongoDBConnection _result;

    void Establish()
    {
        _options.ConnectionResolution.Identity.UseHttp(new Uri("https://platform.example/connections"));
        _resolver.Resolve(Arg.Do<ConnectionRequest>(_ => _request = _), Arg.Any<CancellationToken>())
            .Returns(new ResolvedConnection(Database: "tenant-acme", Identity: new("acme-user", "acme-password")));
    }

    void Because() => _result = _connections.Resolve();

    [Fact] void should_resolve_for_mongodb() => _request!.Storage.ShouldEqual(WellKnownConnectionStorage.MongoDB);
    [Fact] void should_resolve_for_runtime() => _request!.Purpose.ShouldEqual(ConnectionPurpose.Runtime);
    [Fact] void should_resolve_for_the_current_tenant() => _request!.Scope[WellKnownConnectionScopeKeys.Tenant].ShouldEqual("acme");
    [Fact] void should_use_the_resolved_database() => _result.DatabaseName.ShouldEqual("tenant-acme");
    [Fact] void should_create_the_client_for_the_configured_server() => _createdWith!.Server.Host.ShouldEqual("configured");
    [Fact] void should_create_the_client_with_the_resolved_credential() => _createdWith!.Credential.Username.ShouldEqual("acme-user");
    [Fact] void should_follow_authentication_failures() => _createdWith!.ClusterConfigurator.ShouldNotBeNull();
}
