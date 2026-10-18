// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using Cratis.Arc.EntityFrameworkCore.Json;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure.Internal;
using Npgsql.EntityFrameworkCore.PostgreSQL.Migrations;

namespace Cratis.Arc.EntityFrameworkCore;

/// <summary>
/// PostgreSQL specific migrations SQL generator.
/// </summary>
/// <param name="dependencies">The dependencies.</param>
/// <param name="npgsqlSingletonOptions">The Npgsql singleton options.</param>
public class MigrationsSqlGeneratorForPostgreSQL(
        MigrationsSqlGeneratorDependencies dependencies,
#pragma warning disable EF1001 // Internal EF Core API usage.
        INpgsqlSingletonOptions npgsqlSingletonOptions) : NpgsqlMigrationsSqlGenerator(dependencies, npgsqlSingletonOptions)
#pragma warning restore EF1001 // Internal EF Core API usage.
{
    /// <inheritdoc/>
    protected override void Generate(AddColumnOperation operation, IModel? model, MigrationCommandListBuilder builder, bool terminate = true)
    {
        base.Generate(operation, model, builder, terminate || operation.IsJson());
        if (!operation.IsJson()) return;

        var helper = Dependencies.SqlGenerationHelper;
        var table = helper.DelimitIdentifier(operation.Table, operation.Schema);
        var column = helper.DelimitIdentifier(operation.Name);
        var constraintCheck = helper.DelimitIdentifier(LimitIdentifier($"CK_{operation.Table}_{operation.Name}_IsJson"));
        var index = helper.DelimitIdentifier(LimitIdentifier($"IX_{operation.Table}_{operation.Name}_Jsonb"));
        builder
            .Append($"ALTER TABLE {table} ADD CONSTRAINT {constraintCheck} CHECK ({column} IS NULL OR jsonb_typeof(({column})::jsonb) IS NOT NULL)")
            .AppendLine(helper.StatementTerminator)
            .EndCommand()
            .Append($"CREATE INDEX {index} ON {table} USING GIN (({column}::jsonb) jsonb_path_ops)")
            .AppendLine(helper.StatementTerminator)
            .EndCommand();
    }

    static string LimitIdentifier(string identifier)
    {
        const int maxBytes = 63;
        if (Encoding.UTF8.GetByteCount(identifier) <= maxBytes) return identifier;

        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(identifier)))[..16];
        var prefix = new StringBuilder();
        var bytes = 0;
        foreach (var rune in identifier.EnumerateRunes())
        {
            if (bytes + rune.Utf8SequenceLength > maxBytes - hash.Length - 1) break;
            prefix.Append(rune.ToString());
            bytes += rune.Utf8SequenceLength;
        }

        return $"{prefix}_{hash}";
    }
}
