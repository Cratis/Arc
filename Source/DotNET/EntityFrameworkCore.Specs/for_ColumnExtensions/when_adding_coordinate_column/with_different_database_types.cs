// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Cratis.Arc.EntityFrameworkCore.for_ColumnExtensions.when_adding_coordinate_column;

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
        _sqliteMb.CreateTable("TestTable", columns => new { Location = columns.CoordinateColumn(_sqliteMb) });
        _sqliteResult = ((CreateTableOperation)_sqliteMb.Operations[0]).Columns[0];
        _sqlServerMb.CreateTable("TestTable", columns => new { Location = columns.CoordinateColumn(_sqlServerMb) });
        _sqlServerResult = ((CreateTableOperation)_sqlServerMb.Operations[0]).Columns[0];
        _postgreSqlMb.CreateTable("TestTable", columns => new { Location = columns.CoordinateColumn(_postgreSqlMb) });
        _postgreSqlResult = ((CreateTableOperation)_postgreSqlMb.Operations[0]).Columns[0];
    }

    [Fact] void should_set_sqlite_type_to_text() => _sqliteResult.ColumnType.ShouldEqual("text");
    [Fact] void should_set_sql_server_type_to_nvarchar_max() => _sqlServerResult.ColumnType.ShouldEqual("nvarchar(max)");
    [Fact] void should_set_postgresql_type_to_jsonb() => _postgreSqlResult.ColumnType.ShouldEqual("jsonb");
}
