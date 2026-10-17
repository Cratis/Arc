// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Arc.EntityFrameworkCore.Migrations.for_MigrationsSqlGeneratorForPostgreSQL.when_adding_json_column;

public class with_multibyte_identifiers : given.a_migrations_sql_generator
{
    IReadOnlyList<MigrationCommand> _commands;

    void Because() => _commands = Generate(string.Concat(Enumerable.Repeat("表😀", 8)), "Données");

    [Fact] void should_limit_the_constraint_in_utf8_bytes() => Encoding.UTF8.GetByteCount(ConstraintName(_commands)).ShouldBeLessThanOrEqual(63);
    [Fact] void should_limit_the_index_in_utf8_bytes() => Encoding.UTF8.GetByteCount(IndexName(_commands)).ShouldBeLessThanOrEqual(63);
    [Fact] void should_preserve_complete_unicode_characters_in_the_constraint() => Encoding.UTF8.GetString(Encoding.UTF8.GetBytes(ConstraintName(_commands))).ShouldEqual(ConstraintName(_commands));
    [Fact] void should_preserve_complete_unicode_characters_in_the_index() => Encoding.UTF8.GetString(Encoding.UTF8.GetBytes(IndexName(_commands))).ShouldEqual(IndexName(_commands));
}
