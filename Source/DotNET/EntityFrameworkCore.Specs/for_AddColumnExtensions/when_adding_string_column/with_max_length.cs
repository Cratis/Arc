// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Cratis.Arc.EntityFrameworkCore.for_AddColumnExtensions.when_adding_string_column;

public class with_max_length : given.a_migration_builder
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
        _sqliteMb.AddStringColumn("TestColumn", "TestTable", maxLength: 100);
        _sqliteResult = (AddColumnOperation)_sqliteMb.Operations[0];
        _sqlServerMb.AddStringColumn("TestColumn", "TestTable", maxLength: 100);
        _sqlServerResult = (AddColumnOperation)_sqlServerMb.Operations[0];
        _postgreSqlMb.AddStringColumn("TestColumn", "TestTable", maxLength: 100);
        _postgreSqlResult = (AddColumnOperation)_postgreSqlMb.Operations[0];
    }

    [Fact] void should_set_sqlite_type_to_text() => _sqliteResult.ColumnType.ShouldEqual("TEXT");
    [Fact] void should_set_sql_server_type_to_nvarchar_with_length() => _sqlServerResult.ColumnType.ShouldEqual("NVARCHAR(100)");
    [Fact] void should_set_postgresql_type_to_varchar_with_length() => _postgreSqlResult.ColumnType.ShouldEqual("VARCHAR(100)");
}
