// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Data.SqlClient;

namespace Cratis.Arc.EntityFrameworkCore.Connections.for_ResolvedConnectionDbConnectionExtensions.when_applying;

public class to_sql_server_with_a_connection_string : Specification
{
    SqlConnection _connection;
    SqlConnectionStringBuilder _result;

    void Establish() => _connection = new();

    void Because()
    {
        _connection.ApplyResolvedConnection(
            "Server=sql.example;Database=configured",
            new("Server=sql-07.example;Database=tenant-acme;Authentication=Active Directory Managed Identity;User ID=client-id"));
        _result = new(_connection.ConnectionString);
    }

    void Destroy() => _connection.Dispose();

    [Fact] void should_use_the_resolved_server() => _result.DataSource.ShouldEqual("sql-07.example");
    [Fact] void should_use_the_resolved_database() => _result.InitialCatalog.ShouldEqual("tenant-acme");
    [Fact] void should_use_the_resolved_authentication() => _result.Authentication.ShouldEqual(SqlAuthenticationMethod.ActiveDirectoryManagedIdentity);
}
