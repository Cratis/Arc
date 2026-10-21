// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Connections;
using MongoDB.Driver;
using MongoDB.Driver.Authentication.Oidc;

namespace Cratis.Arc.MongoDB.for_ResolvedConnectionMongoDBExtensions.when_converting_to_settings;

public class and_an_access_token_is_resolved : Specification
{
    IOidcCallback _callback;
    MongoClientSettings _settings;

    void Establish() => _callback = Substitute.For<IOidcCallback>();

    void Because() => _settings = new ResolvedConnection(Identity: new(AccessToken: "token"))
        .ToMongoClientSettings(new MongoUrl("mongodb://configured:27017"), _callback);

    [Fact] void should_authenticate_with_oidc() => _settings.Credential.Mechanism.ShouldEqual("MONGODB-OIDC");
}
