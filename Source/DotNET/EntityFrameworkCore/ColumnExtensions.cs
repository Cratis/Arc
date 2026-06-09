// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Numerics;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Migrations.Operations.Builders;

namespace Cratis.Arc.EntityFrameworkCore;

/// <summary>
/// Common column types supported across different database providers.
/// </summary>
public static class ColumnExtensions
{
    /// <summary>
    /// Adds a string column with appropriate database-specific type.
    /// </summary>
    /// <param name="cb">Columns builder.</param>
    /// <param name="mb">Migration builder.</param>
    /// <param name="maxLength">Maximum length of the string column. If null, uses unlimited length.</param>
    /// <param name="nullable">Whether the column should be nullable.</param>
    /// <param name="defaultValue">Optional default value for the column.</param>
    /// <returns>Operation builder for the column.</returns>
    public static OperationBuilder<AddColumnOperation> StringColumn(this ColumnsBuilder cb, MigrationBuilder mb, int? maxLength = null, bool nullable = true, string? defaultValue = null)
    {
        var type = ColumnTypeMappings.GetStringType(mb.GetDatabaseType(), maxLength);
        return cb.Column<string>(type, nullable: nullable, defaultValue: defaultValue);
    }

    /// <summary>
    /// Adds a number column with appropriate database-specific type.
    /// </summary>
    /// <typeparam name="T">The numeric type (char, byte, sbyte, short, ushort, int, uint, long, ulong, float, double, decimal).</typeparam>
    /// <param name="cb">Columns builder.</param>
    /// <param name="mb">Migration builder.</param>
    /// <param name="nullable">Whether the column should be nullable.</param>
    /// <param name="defaultValue">Optional default value for the column.</param>
    /// <returns>Operation builder for the column.</returns>
    public static OperationBuilder<AddColumnOperation> NumberColumn<T>(this ColumnsBuilder cb, MigrationBuilder mb, bool nullable = true, object? defaultValue = null)
        where T : INumber<T>
    {
        var sqlType = ColumnTypeMappings.GetNumberType<T>(mb.GetDatabaseType());
        return cb.Column<T>(sqlType, nullable: nullable, defaultValue: defaultValue);
    }

    /// <summary>
    /// Adds a boolean column with appropriate database-specific type.
    /// </summary>
    /// <param name="cb">Columns builder.</param>
    /// <param name="mb">Migration builder.</param>
    /// <param name="nullable">Whether the column should be nullable.</param>
    /// <param name="defaultValue">Optional default value for the column.</param>
    /// <returns>Operation builder for the column.</returns>
    public static OperationBuilder<AddColumnOperation> BoolColumn(this ColumnsBuilder cb, MigrationBuilder mb, bool nullable = true, bool defaultValue = false)
    {
        var type = ColumnTypeMappings.GetBoolType(mb.GetDatabaseType());
        return cb.Column<bool>(type, nullable: nullable, defaultValue: defaultValue);
    }

    /// <summary>
    /// Adds a column that auto-increments.
    /// </summary>
    /// <param name="cb">Columns builder.</param>
    /// <param name="mb">Migration builder.</param>
    /// <returns>Operation builder for the column.</returns>
    public static OperationBuilder<AddColumnOperation> AutoIncrementColumn(this ColumnsBuilder cb, MigrationBuilder mb)
    {
        var (type, annotationKey, annotationValue) = ColumnTypeMappings.GetAutoIncrementInfo(mb.GetDatabaseType());
        return cb.Column<int>(type, nullable: false).Annotation(annotationKey, annotationValue);
    }

    /// <summary>
    /// Adds a Guid column with appropriate database-specific type.
    /// </summary>
    /// <param name="cb">Columns builder.</param>
    /// <param name="mb">Migration builder.</param>
    /// <param name="nullable">Whether the column should be nullable.</param>
    /// <returns>Operation builder for the column.</returns>
#pragma warning disable CA1720 // Identifier contains type name
    public static OperationBuilder<AddColumnOperation> GuidColumn(this ColumnsBuilder cb, MigrationBuilder mb, bool nullable = true)
    {
        var type = ColumnTypeMappings.GetGuidType(mb.GetDatabaseType());
        return cb.Column<Guid>(type, nullable: nullable);
    }
#pragma warning restore CA1720 // Identifier contains type name

    /// <summary>
    /// Adds a DateTimeOffset column with appropriate database-specific type.
    /// </summary>
    /// <param name="cb">Columns builder.</param>
    /// <param name="mb">Migration builder.</param>
    /// <param name="nullable">Whether the column should be nullable.</param>
    /// <returns>Operation builder for the column.</returns>
    public static OperationBuilder<AddColumnOperation> DateTimeOffsetColumn(this ColumnsBuilder cb, MigrationBuilder mb, bool nullable = true)
    {
        var type = ColumnTypeMappings.GetDateTimeOffsetType(mb.GetDatabaseType());
        return cb.Column<DateTimeOffset>(type, nullable: nullable);
    }
}
