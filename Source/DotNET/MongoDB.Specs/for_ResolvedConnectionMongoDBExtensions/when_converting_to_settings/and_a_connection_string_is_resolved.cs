// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Connections;
using MongoDB.Driver;

namespace Cratis.Arc.MongoDB.for_ResolvedConnectionMongoDBExtensions.when_converting_to_settings;

public class and_a_connection_string_is_resolved : Specification
{
    MongoClientSettings _settings;

    void Because() => _settings = new ResolvedConnection("mongodb://tenant-server:27018").ToMongoClientSettings(new MongoUrl("mongodb://configured:27017"));

    [Fact] void should_use_the_resolved_server() => _settings.Server.Host.ShouldEqual("tenant-server");
    [Fact] void should_use_the_resolved_port() => _settings.Server.Port.ShouldEqual(27018);
}
