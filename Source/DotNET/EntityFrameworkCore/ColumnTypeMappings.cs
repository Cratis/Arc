// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Numerics;

namespace Cratis.Arc.EntityFrameworkCore;

/// <summary>
/// Provides database-specific column type mappings used by both ColumnExtensions and AddColumnExtensions.
/// </summary>
internal static class ColumnTypeMappings
{
    /// <summary>
    /// Gets the appropriate SQL type for a string column.
    /// </summary>
    /// <param name="databaseType">The database type.</param>
    /// <param name="maxLength">Maximum length of the string column. If null, uses unlimited length.</param>
    /// <returns>The SQL type string.</returns>
    internal static string GetStringType(DatabaseType databaseType, int? maxLength) =>
        databaseType switch
        {
            DatabaseType.PostgreSql => maxLength.HasValue ? $"VARCHAR({maxLength})" : "TEXT",
            DatabaseType.SqlServer or DatabaseType.SqlServer2025 => maxLength.HasValue ? $"NVARCHAR({maxLength})" : "NVARCHAR(MAX)",
            _ => "TEXT"
        };

    /// <summary>
    /// Gets the appropriate SQL type for a boolean column.
    /// </summary>
    /// <param name="databaseType">The database type.</param>
    /// <returns>The SQL type string.</returns>
    internal static string GetBoolType(DatabaseType databaseType) =>
        databaseType switch
        {
            DatabaseType.PostgreSql => "BOOLEAN",
            DatabaseType.SqlServer or DatabaseType.SqlServer2025 => "BIT",
            _ => "INTEGER"
        };

    /// <summary>
    /// Gets the appropriate SQL type for a Guid column.
    /// </summary>
    /// <param name="databaseType">The database type.</param>
    /// <returns>The SQL type string.</returns>
    internal static string GetGuidType(DatabaseType databaseType) =>
        databaseType switch
        {
            DatabaseType.PostgreSql => "UUID",
            DatabaseType.SqlServer or DatabaseType.SqlServer2025 => "UNIQUEIDENTIFIER",
            _ => "BLOB"
        };

    /// <summary>
    /// Gets the appropriate SQL type for a DateTimeOffset column.
    /// </summary>
    /// <param name="databaseType">The database type.</param>
    /// <returns>The SQL type string.</returns>
    internal static string GetDateTimeOffsetType(DatabaseType databaseType) =>
        databaseType switch
        {
            DatabaseType.PostgreSql => "TIMESTAMPTZ",
            DatabaseType.SqlServer or DatabaseType.SqlServer2025 => "DATETIMEOFFSET",
            _ => "TEXT"
        };

    /// <summary>
    /// Gets the appropriate SQL type for a JSON column.
    /// </summary>
    /// <param name="databaseType">The database type.</param>
    /// <returns>The SQL type string.</returns>
    internal static string GetJsonType(DatabaseType databaseType) =>
        databaseType switch
        {
            DatabaseType.PostgreSql => "jsonb",
            DatabaseType.SqlServer => "nvarchar(max)",
            DatabaseType.SqlServer2025 => "json",
            _ => "text"
        };

    /// <summary>
    /// Gets the appropriate SQL type and annotation for an auto-increment column.
    /// </summary>
    /// <param name="databaseType">The database type.</param>
    /// <returns>A tuple containing the SQL type, annotation key, and annotation value.</returns>
    internal static (string Type, string AnnotationKey, object AnnotationValue) GetAutoIncrementInfo(DatabaseType databaseType) =>
        databaseType switch
        {
            DatabaseType.PostgreSql => ("INTEGER", "Npgsql:ValueGenerationStrategy", Npgsql.EntityFrameworkCore.PostgreSQL.Metadata.NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
            DatabaseType.SqlServer or DatabaseType.SqlServer2025 => ("BIGINT", "SqlServer:ValueGenerationStrategy", Microsoft.EntityFrameworkCore.Metadata.SqlServerValueGenerationStrategy.IdentityColumn),
            _ => ("INTEGER", "Sqlite:Autoincrement", true)
        };

    /// <summary>
    /// Gets the appropriate SQL type for a numeric type based on the database provider.
    /// </summary>
    /// <typeparam name="T">The numeric type.</typeparam>
    /// <param name="databaseType">The database type.</param>
    /// <returns>The SQL type string.</returns>
    internal static string GetNumberType<T>(DatabaseType databaseType)
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
            DatabaseType.SqlServer or DatabaseType.SqlServer2025 => type.Name switch
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
