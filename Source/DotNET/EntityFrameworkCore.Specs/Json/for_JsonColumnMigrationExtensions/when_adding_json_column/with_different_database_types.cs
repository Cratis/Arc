// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Cratis.Arc.EntityFrameworkCore.Json.for_JsonColumnMigrationExtensions.when_adding_json_column;

public class with_different_database_types : Specification
{
    MigrationBuilder _sqliteMb;
    MigrationBuilder _sqlServerMb;
    MigrationBuilder _postgreSqlMb;
    AddColumnOperation _sqliteResult;
    AddColumnOperation _sqlServerResult;
    AddColumnOperation _postgreSqlResult;

    void Establish()
    {
        SqlServerVersionDetector.Reset();
        _sqliteMb = new MigrationBuilder("Microsoft.EntityFrameworkCore.Sqlite");
        _sqlServerMb = new MigrationBuilder("Microsoft.EntityFrameworkCore.SqlServer");
        _postgreSqlMb = new MigrationBuilder("Npgsql.EntityFrameworkCore.PostgreSQL");
    }

    void Because()
    {
        _sqliteMb.AddJsonColumn<object>("Data", "TestTable");
        _sqliteResult = (AddColumnOperation)_sqliteMb.Operations[0];
        _sqlServerMb.AddJsonColumn<object>("Data", "TestTable");
        _sqlServerResult = (AddColumnOperation)_sqlServerMb.Operations[0];
        _postgreSqlMb.AddJsonColumn<object>("Data", "TestTable");
        _postgreSqlResult = (AddColumnOperation)_postgreSqlMb.Operations[0];
    }

    [Fact] void should_set_sqlite_type_to_text() => _sqliteResult.ColumnType.ShouldEqual("text");
    [Fact] void should_set_sql_server_type_to_nvarchar_max() => _sqlServerResult.ColumnType.ShouldEqual("nvarchar(max)");
    [Fact] void should_set_postgresql_type_to_jsonb() => _postgreSqlResult.ColumnType.ShouldEqual("jsonb");
    [Fact] void should_mark_sqlite_result_as_json() => _sqliteResult.IsJson().ShouldBeTrue();
    [Fact] void should_mark_sql_server_result_as_json() => _sqlServerResult.IsJson().ShouldBeTrue();
    [Fact] void should_mark_postgresql_result_as_json() => _postgreSqlResult.IsJson().ShouldBeTrue();
    [Fact] void should_not_be_nullable_on_sqlite() => _sqliteResult.IsNullable.ShouldBeFalse();
    [Fact] void should_not_be_nullable_on_sql_server() => _sqlServerResult.IsNullable.ShouldBeFalse();
    [Fact] void should_not_be_nullable_on_postgresql() => _postgreSqlResult.IsNullable.ShouldBeFalse();
}
