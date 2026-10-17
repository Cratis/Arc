// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Arc.EntityFrameworkCore.Migrations.for_MigrationsSqlGeneratorForPostgreSQL.when_adding_json_column;

public class with_default_store_type : given.a_migrations_sql_generator
{
    IReadOnlyList<MigrationCommand> _commands;

    void Because() => _commands = Generate("Observers", "TypeSetControl");

    [Fact] void should_emit_separate_column_constraint_and_index_commands() => _commands.Count.ShouldEqual(3);
    [Fact] void should_terminate_every_command() => _commands.ShouldEachConformTo(command => command.CommandText.TrimEnd().EndsWith(';'));
    [Fact] void should_create_a_jsonb_column() => _commands[0].CommandText.ShouldContain("ADD \"TypeSetControl\" jsonb NOT NULL;");
    [Fact] void should_emit_a_terminated_check_constraint() => _commands[1].CommandText.TrimEnd().ShouldEqual("ALTER TABLE \"Observers\" ADD CONSTRAINT \"CK_Observers_TypeSetControl_IsJson\" CHECK (\"TypeSetControl\" IS NULL OR jsonb_typeof((\"TypeSetControl\")::jsonb) IS NOT NULL);");
    [Fact] void should_delimit_the_index_name_and_index_a_jsonb_expression() => _commands[2].CommandText.TrimEnd().ShouldEqual("CREATE INDEX \"IX_Observers_TypeSetControl_Jsonb\" ON \"Observers\" USING GIN ((\"TypeSetControl\"::jsonb) jsonb_path_ops);");
}
