// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Connections;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Arc.EntityFrameworkCore.for_EntityFrameworkCoreArcBuilderExtensions;

public class when_connection_resolution_is_configured : given.an_arc_host
{
    int _result;

    void Establish() => _builder.AddCratisArc(configureBuilder: arc => arc.WithEntityFrameworkCore(
        configureOptions: options =>
        {
            options.ConnectionString = "Data Source=:memory:";
            options.ConnectionResolution.Connection.Resolver = ConnectionResolverType.Template;
            options.ConnectionResolution.Connection.Template.ConnectionString = "Data Source=:memory:;Pooling=False";
        }));

    async Task Because() => _result = await Query();

    [Fact] void should_query_the_read_only_context() => _result.ShouldEqual(1);

    [Fact] void should_apply_the_resolved_connection_when_opening() =>
        new SqliteConnectionStringBuilder(_context.Database.GetDbConnection().ConnectionString).Pooling.ShouldBeFalse();
}
