// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Connections;

namespace Cratis.Arc.MongoDB.for_MongoDBConnections.when_resolving;

public class with_only_an_identity_resolved : given.mongodb_connections
{
    MongoDBConnection _result;

    void Establish()
    {
        _options.ConnectionResolution.Identity.UseHttp(new Uri("https://platform.example/connections"));
        _resolver.Resolve(Arg.Any<ConnectionRequest>(), Arg.Any<CancellationToken>()).Returns(new ResolvedConnection(Identity: new(AccessToken: "token")));
    }

    void Because() => _result = _connections.Resolve();

    [Fact] void should_keep_the_database_from_the_database_name_resolver() => _result.DatabaseName.ShouldEqual("app+acme");
    [Fact] void should_authenticate_with_oidc() => _createdWith!.Credential.Mechanism.ShouldEqual("MONGODB-OIDC");
}
