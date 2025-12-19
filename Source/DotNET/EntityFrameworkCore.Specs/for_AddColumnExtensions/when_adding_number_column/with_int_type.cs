// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Cratis.Arc.EntityFrameworkCore.for_AddColumnExtensions.when_adding_number_column;

public class with_int_type : given.a_migration_builder
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
        _sqliteMb.AddNumberColumn<int>("Count", "TestTable");
        _sqliteResult = (AddColumnOperation)_sqliteMb.Operations[0];
        _sqlServerMb.AddNumberColumn<int>("Count", "TestTable");
        _sqlServerResult = (AddColumnOperation)_sqlServerMb.Operations[0];
        _postgreSqlMb.AddNumberColumn<int>("Count", "TestTable");
        _postgreSqlResult = (AddColumnOperation)_postgreSqlMb.Operations[0];
    }

    [Fact] void should_set_sqlite_type_to_integer() => _sqliteResult.ColumnType.ShouldEqual("INTEGER");
    [Fact] void should_set_sql_server_type_to_int() => _sqlServerResult.ColumnType.ShouldEqual("INT");
    [Fact] void should_set_postgresql_type_to_integer() => _postgreSqlResult.ColumnType.ShouldEqual("INTEGER");
}
