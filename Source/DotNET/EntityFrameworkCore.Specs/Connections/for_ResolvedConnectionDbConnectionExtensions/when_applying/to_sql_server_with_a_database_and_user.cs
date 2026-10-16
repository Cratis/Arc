// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Data.SqlClient;

namespace Cratis.Arc.EntityFrameworkCore.Connections.for_ResolvedConnectionDbConnectionExtensions.when_applying;

public class to_sql_server_with_a_database_and_user : Specification
{
    SqlConnection _connection;
    SqlConnectionStringBuilder _result;

    void Establish() => _connection = new();

    void Because()
    {
        _connection.ApplyResolvedConnection(
            "Server=sql.example;Database=configured;User ID=configured-user;Password=configured-password;Encrypt=true",
            new(Database: "tenant-acme", Identity: new("acme-user", "acme-password")));
        _result = new(_connection.ConnectionString);
    }

    void Destroy() => _connection.Dispose();

    [Fact] void should_keep_the_configured_server() => _result.DataSource.ShouldEqual("sql.example");
    [Fact] void should_use_the_resolved_database() => _result.InitialCatalog.ShouldEqual("tenant-acme");
    [Fact] void should_use_the_resolved_user() => _result.UserID.ShouldEqual("acme-user");
    [Fact] void should_use_the_resolved_password() => _result.Password.ShouldEqual("acme-password");
    [Fact] void should_keep_other_settings() => _result.Encrypt.ShouldEqual(SqlConnectionEncryptOption.Mandatory);
    [Fact] void should_not_set_an_access_token() => _connection.AccessToken.ShouldBeNull();
}
