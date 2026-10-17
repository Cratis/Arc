// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Arc.EntityFrameworkCore.Migrations.for_MigrationsSqlGeneratorForPostgreSQL.when_adding_json_column;

public class with_quoted_identifiers : given.a_migrations_sql_generator
{
    IReadOnlyList<MigrationCommand> _commands;

    void Because() => _commands = Generate("Odd\"Table", "Json\"Data", schema: "App\"Schema");

    [Fact] void should_delimit_the_schema_table_and_column() => _commands[1].CommandText.ShouldContain("ALTER TABLE \"App\"\"Schema\".\"Odd\"\"Table\" ADD CONSTRAINT \"CK_Odd\"\"Table_Json\"\"Data_IsJson\"");
    [Fact] void should_escape_the_index_identifier() => _commands[2].CommandText.ShouldContain("CREATE INDEX \"IX_Odd\"\"Table_Json\"\"Data_Jsonb\"");
    [Fact] void should_escape_the_indexed_column() => _commands[2].CommandText.ShouldContain("((\"Json\"\"Data\"::jsonb) jsonb_path_ops)");
}
