// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Numerics;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Migrations.Operations.Builders;

namespace Cratis.Arc.EntityFrameworkCore;

/// <summary>
/// Extension methods for adding columns to existing tables with database-agnostic types.
/// </summary>
public static class AddColumnExtensions
{
    /// <summary>
    /// Adds a string column to an existing table with appropriate database-specific type.
    /// </summary>
    /// <param name="mb">Migration builder.</param>
    /// <param name="name">The name of the column.</param>
    /// <param name="table">The name of the table.</param>
    /// <param name="maxLength">Maximum length of the string column. If null, uses unlimited length.</param>
    /// <param name="nullable">Whether the column should be nullable.</param>
    /// <param name="defaultValue">Optional default value for the column.</param>
    /// <param name="schema">The schema of the table.</param>
    /// <returns>Operation builder for the column.</returns>
    public static OperationBuilder<AddColumnOperation> AddStringColumn(
        this MigrationBuilder mb,
        string name,
        string table,
        int? maxLength = null,
        bool nullable = true,
        string? defaultValue = null,
        string? schema = null)
    {
        var type = mb.GetDatabaseType() switch
        {
            DatabaseType.PostgreSql => maxLength.HasValue ? $"VARCHAR({maxLength})" : "TEXT",
            DatabaseType.SqlServer => maxLength.HasValue ? $"NVARCHAR({maxLength})" : "NVARCHAR(MAX)",
            _ => "TEXT"
        };

        return mb.AddColumn<string>(name, table, type: type, schema: schema, nullable: nullable, defaultValue: defaultValue);
    }

    /// <summary>
    /// Adds a number column to an existing table with appropriate database-specific type.
    /// </summary>
    /// <typeparam name="T">The numeric type (char, byte, sbyte, short, ushort, int, uint, long, ulong, float, double, decimal).</typeparam>
    /// <param name="mb">Migration builder.</param>
    /// <param name="name">The name of the column.</param>
    /// <param name="table">The name of the table.</param>
    /// <param name="nullable">Whether the column should be nullable.</param>
    /// <param name="defaultValue">Optional default value for the column.</param>
    /// <param name="schema">The schema of the table.</param>
    /// <returns>Operation builder for the column.</returns>
    public static OperationBuilder<AddColumnOperation> AddNumberColumn<T>(
        this MigrationBuilder mb,
        string name,
        string table,
        bool nullable = true,
        object? defaultValue = null,
        string? schema = null)
        where T : INumber<T>
    {
        var sqlType = GetSqlTypeForNumber<T>(mb.GetDatabaseType());
        return mb.AddColumn<T>(name, table, type: sqlType, schema: schema, nullable: nullable, defaultValue: defaultValue);
    }

    /// <summary>
    /// Adds a boolean column to an existing table with appropriate database-specific type.
    /// </summary>
    /// <param name="mb">Migration builder.</param>
    /// <param name="name">The name of the column.</param>
    /// <param name="table">The name of the table.</param>
    /// <param name="nullable">Whether the column should be nullable.</param>
    /// <param name="defaultValue">Optional default value for the column.</param>
    /// <param name="schema">The schema of the table.</param>
    /// <returns>Operation builder for the column.</returns>
    public static OperationBuilder<AddColumnOperation> AddBoolColumn(
        this MigrationBuilder mb,
        string name,
        string table,
        bool nullable = true,
        bool defaultValue = false,
        string? schema = null)
    {
        var type = mb.GetDatabaseType() switch
        {
            DatabaseType.PostgreSql => "BOOLEAN",
            DatabaseType.SqlServer => "BIT",
            _ => "INTEGER"
        };

        return mb.AddColumn<bool>(name, table, type: type, schema: schema, nullable: nullable, defaultValue: defaultValue);
    }

    /// <summary>
    /// Adds an auto-increment column to an existing table.
    /// </summary>
    /// <param name="mb">Migration builder.</param>
    /// <param name="name">The name of the column.</param>
    /// <param name="table">The name of the table.</param>
    /// <param name="schema">The schema of the table.</param>
    /// <returns>Operation builder for the column.</returns>
    public static OperationBuilder<AddColumnOperation> AddAutoIncrementColumn(
        this MigrationBuilder mb,
        string name,
        string table,
        string? schema = null) =>
        mb.GetDatabaseType() switch
        {
            DatabaseType.PostgreSql => mb.AddColumn<int>(name, table, type: "INTEGER", schema: schema, nullable: false)
                .Annotation("Npgsql:ValueGenerationStrategy", Npgsql.EntityFrameworkCore.PostgreSQL.Metadata.NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
            DatabaseType.SqlServer => mb.AddColumn<int>(name, table, type: "BIGINT", schema: schema, nullable: false)
                .Annotation("SqlServer:ValueGenerationStrategy", Microsoft.EntityFrameworkCore.Metadata.SqlServerValueGenerationStrategy.IdentityColumn),
            _ => mb.AddColumn<int>(name, table, type: "INTEGER", schema: schema, nullable: false)
                .Annotation("Sqlite:Autoincrement", true)
        };

    /// <summary>
    /// Adds a Guid column to an existing table with appropriate database-specific type.
    /// </summary>
    /// <param name="mb">Migration builder.</param>
    /// <param name="name">The name of the column.</param>
    /// <param name="table">The name of the table.</param>
    /// <param name="nullable">Whether the column should be nullable.</param>
    /// <param name="schema">The schema of the table.</param>
    /// <returns>Operation builder for the column.</returns>
#pragma warning disable CA1720 // Identifier contains type name
    public static OperationBuilder<AddColumnOperation> AddGuidColumn(
        this MigrationBuilder mb,
        string name,
        string table,
        bool nullable = true,
        string? schema = null)
    {
        var type = mb.GetDatabaseType() switch
        {
            DatabaseType.PostgreSql => "UUID",
            DatabaseType.SqlServer => "UNIQUEIDENTIFIER",
            _ => "BLOB"
        };

        return mb.AddColumn<Guid>(name, table, type: type, schema: schema, nullable: nullable);
    }
#pragma warning restore CA1720 // Identifier contains type name

    /// <summary>
    /// Adds a DateTimeOffset column to an existing table with appropriate database-specific type.
    /// </summary>
    /// <param name="mb">Migration builder.</param>
    /// <param name="name">The name of the column.</param>
    /// <param name="table">The name of the table.</param>
    /// <param name="nullable">Whether the column should be nullable.</param>
    /// <param name="schema">The schema of the table.</param>
    /// <returns>Operation builder for the column.</returns>
    public static OperationBuilder<AddColumnOperation> AddDateTimeOffsetColumn(
        this MigrationBuilder mb,
        string name,
        string table,
        bool nullable = true,
        string? schema = null)
    {
        var type = mb.GetDatabaseType() switch
        {
            DatabaseType.PostgreSql => "TIMESTAMPTZ",
            DatabaseType.SqlServer => "DATETIMEOFFSET",
            _ => "TEXT"
        };

        return mb.AddColumn<DateTimeOffset>(name, table, type: type, schema: schema, nullable: nullable);
    }

    /// <summary>
    /// Gets the appropriate SQL type for a numeric type based on the database provider.
    /// </summary>
    /// <typeparam name="T">The numeric type.</typeparam>
    /// <param name="databaseType">The database type.</param>
    /// <returns>The SQL type string.</returns>
    static string GetSqlTypeForNumber<T>(DatabaseType databaseType)
        where T : INumber<T>
    {
        var type = typeof(T);

        return databaseType switch
        {
            DatabaseType.PostgreSql => type.Name switch
            {
                nameof(Char) => "SMALLINT",
                nameof(Byte) => "SMALLINT",
                nameof(SByte) => "SMALLINT",
                nameof(Int16) => "SMALLINT",
                nameof(UInt16) => "INTEGER",
                nameof(Int32) => "INTEGER",
                nameof(UInt32) => "BIGINT",
                nameof(Int64) => "BIGINT",
                nameof(UInt64) => "NUMERIC(20,0)",
                nameof(Single) => "REAL",
                nameof(Double) => "DOUBLE PRECISION",
                nameof(Decimal) => "DECIMAL",
                _ => "INTEGER"
            },
            DatabaseType.SqlServer => type.Name switch
            {
                nameof(Char) => "SMALLINT",
                nameof(Byte) => "TINYINT",
                nameof(SByte) => "SMALLINT",
                nameof(Int16) => "SMALLINT",
                nameof(UInt16) => "INT",
                nameof(Int32) => "INT",
                nameof(UInt32) => "BIGINT",
                nameof(Int64) => "BIGINT",
                nameof(UInt64) => "DECIMAL(20,0)",
                nameof(Single) => "REAL",
                nameof(Double) => "FLOAT",
                nameof(Decimal) => "DECIMAL(18,2)",
                _ => "INT"
            },
            _ => type.Name switch // SQLite and others
            {
                nameof(Single) => "REAL",
                nameof(Double) => "REAL",
                nameof(Decimal) => "REAL",
                _ => "INTEGER"
            }
        };
    }
}
