// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Connections;
using Npgsql;

namespace Cratis.Arc.EntityFrameworkCore.Connections.for_ResolvedConnectionDbConnectionExtensions.when_applying;

public class nothing_resolved : Specification
{
    NpgsqlConnection _connection;
    NpgsqlConnectionStringBuilder _result;

    void Establish() => _connection = new();

    void Because()
    {
        _connection.ApplyResolvedConnection("Host=pg.example;Database=configured;Username=user;Password=password", ResolvedConnection.Unchanged);
        _result = new(_connection.ConnectionString);
    }

    void Destroy() => _connection.Dispose();

    [Fact] void should_keep_the_database() => _result.Database.ShouldEqual("configured");
    [Fact] void should_keep_the_user() => _result.Username.ShouldEqual("user");
    [Fact] void should_keep_the_password() => _result.Password.ShouldEqual("password");
}
