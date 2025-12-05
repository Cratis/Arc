// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Cratis.Arc.EntityFrameworkCore.for_AddColumnExtensions.when_adding_datetimeoffset_column;

public class with_different_database_types : given.a_migration_builder
{
    MigrationBuilder _sqliteMb;
    MigrationBuilder _sqlServerMb;
    MigrationBuilder _postgreSqlMb;
    AddColumnOperation _sqliteResult;
    AddColumnOperation _sqlServerResult;
    AddColumnOperation _postgreSqlResult;

    void Establish()
    {
        _sqliteMb = CreateSqliteMigrationBuilder();
        _sqlServerMb = CreateSqlServerMigrationBuilder();
        _postgreSqlMb = CreatePostgreSqlMigrationBuilder();
    }

    void Because()
    {
        _sqliteMb.AddDateTimeOffsetColumn("CreatedAt", "TestTable");
        _sqliteResult = (AddColumnOperation)_sqliteMb.Operations[0];
        _sqlServerMb.AddDateTimeOffsetColumn("CreatedAt", "TestTable");
        _sqlServerResult = (AddColumnOperation)_sqlServerMb.Operations[0];
        _postgreSqlMb.AddDateTimeOffsetColumn("CreatedAt", "TestTable");
        _postgreSqlResult = (AddColumnOperation)_postgreSqlMb.Operations[0];
    }

    [Fact] void should_set_sqlite_type_to_text() => _sqliteResult.ColumnType.ShouldEqual("TEXT");
    [Fact] void should_set_sql_server_type_to_datetimeoffset() => _sqlServerResult.ColumnType.ShouldEqual("DATETIMEOFFSET");
    [Fact] void should_set_postgresql_type_to_timestamptz() => _postgreSqlResult.ColumnType.ShouldEqual("TIMESTAMPTZ");
}
