// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Cratis.Arc.EntityFrameworkCore.for_AddColumnExtensions.when_adding_string_column;

public class with_sql_server : given.a_migration_builder
{
    MigrationBuilder _migrationBuilder;
    AddColumnOperation _result;

    void Establish() => _migrationBuilder = CreateSqlServerMigrationBuilder();

    void Because()
    {
        _migrationBuilder.AddStringColumn("TestColumn", "TestTable");
        _result = (AddColumnOperation)_migrationBuilder.Operations[0];
    }

    [Fact] void should_set_column_name() => _result.Name.ShouldEqual("TestColumn");
    [Fact] void should_set_table_name() => _result.Table.ShouldEqual("TestTable");
    [Fact] void should_set_type_to_nvarchar_max() => _result.ColumnType.ShouldEqual("NVARCHAR(MAX)");
    [Fact] void should_be_nullable_by_default() => _result.IsNullable.ShouldBeTrue();
}
