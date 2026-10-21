// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Connections;
using MongoDB.Driver;

namespace Cratis.Arc.MongoDB.for_ResolvedConnectionMongoDBExtensions.when_converting_to_settings;

public class and_nothing_is_resolved : Specification
{
    MongoClientSettings _settings;

    void Because() => _settings = ResolvedConnection.Unchanged.ToMongoClientSettings(new MongoUrl("mongodb://user:password@configured:27017"));

    [Fact] void should_use_the_configured_server() => _settings.Server.Host.ShouldEqual("configured");
    [Fact] void should_keep_the_configured_credential() => _settings.Credential.Username.ShouldEqual("user");
}
