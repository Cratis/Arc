// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Connections;
using MongoDB.Driver;

namespace Cratis.Arc.MongoDB.for_ResolvedConnectionMongoDBExtensions.when_converting_to_settings;

public class and_a_user_name_and_password_are_resolved : Specification
{
    MongoClientSettings _settings;
    MongoClientSettings _settingsWithAuthSource;

    void Because()
    {
        var resolved = new ResolvedConnection(Identity: new("tenant-user", "tenant-password"));
        _settings = resolved.ToMongoClientSettings(new MongoUrl("mongodb://configured:27017"));
        _settingsWithAuthSource = resolved.ToMongoClientSettings(new MongoUrl("mongodb://configured:27017/?authSource=tenants"));
    }

    [Fact] void should_authenticate_as_the_resolved_user() => _settings.Credential.Username.ShouldEqual("tenant-user");
    [Fact] void should_authenticate_against_admin_by_default() => _settings.Credential.Source.ShouldEqual(ResolvedConnectionMongoDBExtensions.DefaultAuthenticationSource);
    [Fact] void should_authenticate_against_the_configured_source() => _settingsWithAuthSource.Credential.Source.ShouldEqual("tenants");
}
