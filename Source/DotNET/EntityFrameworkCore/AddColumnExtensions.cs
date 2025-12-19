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
        var type = ColumnTypeMappings.GetStringType(mb.GetDatabaseType(), maxLength);
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
        var sqlType = ColumnTypeMappings.GetNumberType<T>(mb.GetDatabaseType());
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
        var type = ColumnTypeMappings.GetBoolType(mb.GetDatabaseType());
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
        string? schema = null)
    {
        var (type, annotationKey, annotationValue) = ColumnTypeMappings.GetAutoIncrementInfo(mb.GetDatabaseType());
        return mb.AddColumn<int>(name, table, type: type, schema: schema, nullable: false)
            .Annotation(annotationKey, annotationValue);
    }

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
        var type = ColumnTypeMappings.GetGuidType(mb.GetDatabaseType());
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
        var type = ColumnTypeMappings.GetDateTimeOffsetType(mb.GetDatabaseType());
        return mb.AddColumn<DateTimeOffset>(name, table, type: type, schema: schema, nullable: nullable);
    }
}
