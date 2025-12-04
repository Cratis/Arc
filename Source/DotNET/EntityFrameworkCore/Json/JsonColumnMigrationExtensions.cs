// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Migrations.Operations.Builders;

namespace Cratis.Arc.EntityFrameworkCore.Json;

/// <summary>
/// Extension methods for working with Json columns.
/// </summary>
public static class JsonColumnMigrationExtensions
{
    /// <summary>
    /// The annotation used to store the column type information.
    /// </summary>
    public const string CratisColumnTypeAnnotation = "cratis:ColumnType";

    /// <summary>
    /// The column type used to indicate a JSON column.
    /// </summary>
    public const string JsonColumnType = "json";

    /// <summary>
    /// Adds a Json column to the specified table.
    /// </summary>
    /// <param name="cb">Columns builder.</param>
    /// <param name="mb">Migration builder.</param>
    /// <typeparam name="TProperty">Type of property.</typeparam>
    /// <returns>Operation builder for the column.</returns>
    public static OperationBuilder<AddColumnOperation> JsonColumn<TProperty>(this ColumnsBuilder cb, MigrationBuilder mb) =>
        (mb.GetDatabaseType() switch
        {
            DatabaseType.PostgreSql => cb.Column<TProperty>("jsonb", nullable: false),
            DatabaseType.SqlServer => cb.Column<TProperty>("nvarchar(max)", nullable: false),
            _ => cb.Column<TProperty>("text", nullable: false)
        }).Annotation("cratis:ColumnType", JsonColumnType);

    /// <summary>
    /// Adds a Json column to an existing table.
    /// </summary>
    /// <param name="mb">Migration builder.</param>
    /// <param name="name">The name of the column.</param>
    /// <param name="table">The name of the table.</param>
    /// <param name="schema">The schema of the table.</param>
    /// <typeparam name="TProperty">Type of property.</typeparam>
    /// <returns>Operation builder for the column.</returns>
    public static OperationBuilder<AddColumnOperation> AddJsonColumn<TProperty>(
        this MigrationBuilder mb,
        string name,
        string table,
        string? schema = null)
    {
        var type = mb.GetDatabaseType() switch
        {
            DatabaseType.PostgreSql => "jsonb",
            DatabaseType.SqlServer => "nvarchar(max)",
            _ => "text"
        };

        return mb.AddColumn<TProperty>(name, table, type: type, schema: schema, nullable: false)
            .Annotation(CratisColumnTypeAnnotation, JsonColumnType);
    }

    /// <summary>
    /// Checks if the column is a JSON column.
    /// </summary>
    /// <param name="column">The column to check.</param>
    /// <returns>True if the column is a JSON column.</returns>
    public static bool IsJson(this AddColumnOperation column) => column[CratisColumnTypeAnnotation]?.ToString() == JsonColumnType;
}
