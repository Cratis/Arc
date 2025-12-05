// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Cratis.Arc.EntityFrameworkCore.for_AddColumnExtensions.when_adding_guid_column;

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
        _sqliteMb.AddGuidColumn("Id", "TestTable");
        _sqliteResult = (AddColumnOperation)_sqliteMb.Operations[0];
        _sqlServerMb.AddGuidColumn("Id", "TestTable");
        _sqlServerResult = (AddColumnOperation)_sqlServerMb.Operations[0];
        _postgreSqlMb.AddGuidColumn("Id", "TestTable");
        _postgreSqlResult = (AddColumnOperation)_postgreSqlMb.Operations[0];
    }

    [Fact] void should_set_sqlite_type_to_blob() => _sqliteResult.ColumnType.ShouldEqual("BLOB");
    [Fact] void should_set_sql_server_type_to_uniqueidentifier() => _sqlServerResult.ColumnType.ShouldEqual("UNIQUEIDENTIFIER");
    [Fact] void should_set_postgresql_type_to_uuid() => _postgreSqlResult.ColumnType.ShouldEqual("UUID");
    [Fact] void should_be_nullable_by_default_on_sqlite() => _sqliteResult.IsNullable.ShouldBeTrue();
    [Fact] void should_be_nullable_by_default_on_sql_server() => _sqlServerResult.IsNullable.ShouldBeTrue();
    [Fact] void should_be_nullable_by_default_on_postgresql() => _postgreSqlResult.IsNullable.ShouldBeTrue();
}
