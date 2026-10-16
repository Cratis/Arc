// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Npgsql;

namespace Cratis.Arc.EntityFrameworkCore.Connections.for_ResolvedConnectionDbConnectionExtensions.when_applying;

public class to_postgresql_with_an_access_token : Specification
{
    NpgsqlConnection _connection;
    NpgsqlConnectionStringBuilder _result;

    void Establish() => _connection = new();

    void Because()
    {
        _connection.ApplyResolvedConnection(
            "Host=pg.example;Database=configured;Username=configured-user;Password=configured-password",
            new(Database: "tenant_acme", Identity: new("acme-identity", AccessToken: "the-token")));
        _result = new(_connection.ConnectionString);
    }

    void Destroy() => _connection.Dispose();

    [Fact] void should_keep_the_configured_host() => _result.Host.ShouldEqual("pg.example");
    [Fact] void should_use_the_resolved_database() => _result.Database.ShouldEqual("tenant_acme");
    [Fact] void should_use_the_resolved_user() => _result.Username.ShouldEqual("acme-identity");
    [Fact] void should_use_the_access_token_as_password() => _result.Password.ShouldEqual("the-token");
}
