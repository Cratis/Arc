// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Cratis.Arc.EntityFrameworkCore.Migrations.for_MigrationsSqlGeneratorForPostgreSQL;

public class when_adding_non_json_column : given.a_migrations_sql_generator
{
    IReadOnlyList<MigrationCommand> _commands;

    void Because() => _commands = _generator.Generate([new AddColumnOperation { Table = "Documents", Name = "Title", ClrType = typeof(string), ColumnType = "text", IsNullable = true }]);

    [Fact] void should_emit_only_the_column_command() => _commands.Count.ShouldEqual(1);
    [Fact] void should_preserve_the_provider_sql() => _commands[0].CommandText.TrimEnd().ShouldEqual("ALTER TABLE \"Documents\" ADD \"Title\" text;");
}
