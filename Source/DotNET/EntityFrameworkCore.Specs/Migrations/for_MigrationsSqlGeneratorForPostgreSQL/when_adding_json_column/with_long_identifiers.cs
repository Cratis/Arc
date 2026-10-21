// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Arc.EntityFrameworkCore.Migrations.for_MigrationsSqlGeneratorForPostgreSQL.when_adding_json_column;

public class with_long_identifiers : given.a_migrations_sql_generator
{
    IReadOnlyList<MigrationCommand> _commands;
    IReadOnlyList<MigrationCommand> _repeated;
    IReadOnlyList<MigrationCommand> _different;

    void Because()
    {
        var table = new string('T', 60);
        var column = new string('C', 60);
        _commands = Generate(table, column);
        _repeated = Generate(table, column);
        _different = Generate(table, $"{column[..59]}D");
    }

    [Fact] void should_limit_the_constraint_to_63_bytes() => Encoding.UTF8.GetByteCount(ConstraintName(_commands)).ShouldBeLessThanOrEqual(63);
    [Fact] void should_limit_the_index_to_63_bytes() => Encoding.UTF8.GetByteCount(IndexName(_commands)).ShouldBeLessThanOrEqual(63);
    [Fact] void should_generate_stable_constraint_names() => ConstraintName(_commands).ShouldEqual(ConstraintName(_repeated));
    [Fact] void should_generate_stable_index_names() => IndexName(_commands).ShouldEqual(IndexName(_repeated));
    [Fact] void should_distinguish_constraints_with_the_same_prefix() => ConstraintName(_commands).ShouldNotEqual(ConstraintName(_different));
    [Fact] void should_distinguish_indexes_with_the_same_prefix() => IndexName(_commands).ShouldNotEqual(IndexName(_different));
}
