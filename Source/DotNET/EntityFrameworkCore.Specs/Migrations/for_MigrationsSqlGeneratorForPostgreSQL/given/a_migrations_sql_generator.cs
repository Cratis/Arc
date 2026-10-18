// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Cratis.Arc.EntityFrameworkCore.Migrations.for_MigrationsSqlGeneratorForPostgreSQL.given;

public class a_migrations_sql_generator : Specification
{
    protected DbContext _context;
    protected IMigrationsSqlGenerator _generator;

    void Establish()
    {
        var options = new DbContextOptionsBuilder()
            .UseNpgsql("Host=localhost;Database=sql_generation")
            .ReplaceService<IMigrationsSqlGenerator, MigrationsSqlGeneratorForPostgreSQL>()
            .Options;
        _context = new DbContext(options);
        _generator = _context.GetService<IMigrationsSqlGenerator>();
    }

    void Destroy() => _context.Dispose();

    protected IReadOnlyList<MigrationCommand> Generate(string table, string column, string? storeType = null, string? schema = null)
    {
        var migration = new MigrationBuilder(_context.Database.ProviderName);
        migration.AddJsonColumn<string>(column, table, schema);
        var operation = (AddColumnOperation)migration.Operations[0];
        if (storeType is not null) operation.ColumnType = storeType;

        return _generator.Generate(migration.Operations);
    }

    protected static string ConstraintName(IReadOnlyList<MigrationCommand> commands) =>
        commands[1].CommandText.Split('"')[3];

    protected static string IndexName(IReadOnlyList<MigrationCommand> commands) =>
        commands[2].CommandText.Split('"')[1];
}
