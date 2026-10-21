// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Data.SqlClient;

namespace Cratis.Arc.EntityFrameworkCore.Connections.for_ResolvedConnectionDbConnectionExtensions.when_applying;

public class to_sql_server_with_an_access_token : Specification
{
    SqlConnection _connection;

    void Establish() => _connection = new();

    void Because() => _connection.ApplyResolvedConnection(
        "Server=sql.example;Database=configured",
        new(Identity: new(AccessToken: "the-token")));

    void Destroy() => _connection.Dispose();

    [Fact] void should_set_the_access_token() => _connection.AccessToken.ShouldEqual("the-token");
}
