---
title: Common column types
description: Reference for Arc CreateTable column helpers and their SQL type mappings.
---

Arc's migration helpers select SQL type names for PostgreSQL, SQL Server, and SQLite. They do not configure model value converters or guarantee that an entire migration is portable.

## Available column extension methods

These extend EF's `ColumnsBuilder` and require the current `MigrationBuilder` as their first argument. Import `Cratis.Arc.EntityFrameworkCore`; JSON helpers additionally require `Cratis.Arc.EntityFrameworkCore.Json`.

| Method | Additional arguments | Nullability |
| --- | --- | --- |
| `StringColumn(mb)` | `maxLength: int?`, `nullable: bool`, `defaultValue: string?` | Nullable by default |
| `NumberColumn<T>(mb)` | `nullable: bool`, `defaultValue: object?`; `T : INumber<T>` | Nullable by default |
| `BoolColumn(mb)` | `nullable: bool`, `defaultValue: bool = false` | Nullable by default |
| `AutoIncrementColumn(mb)` | None | Non-nullable |
| `GuidColumn(mb)` | `nullable: bool` | Nullable by default |
| `DateTimeOffsetColumn(mb)` | `nullable: bool` | Nullable by default |
| `JsonColumn<T>(mb)` | None | Non-nullable |

There are no `CoordinateColumn`, `PointColumn`, `LineStringColumn`, or `PolygonColumn` create-table helpers. Geometry `Add*Column` methods are a separate API; see [adding columns](./migrations-add-columns.md).

## Database-specific type mappings

| Value | PostgreSQL | SQL Server | SQLite |
| --- | --- | --- | --- |
| String with length | `VARCHAR(n)` | `NVARCHAR(n)` | `TEXT` |
| Unlimited string | `TEXT` | `NVARCHAR(MAX)` | `TEXT` |
| Boolean | `BOOLEAN` | `BIT` | `INTEGER` |
| Guid | `UUID` | `UNIQUEIDENTIFIER` | `BLOB` |
| DateTimeOffset | `TIMESTAMPTZ` | `DATETIMEOFFSET` | `TEXT` |
| JSON | `jsonb` | `nvarchar(max)` | `text` |
| Auto-increment | `INTEGER` + identity-by-default annotation | `BIGINT` + identity annotation | `INTEGER` + autoincrement annotation |

`NumberColumn<T>` selects integer/floating/decimal types by `T`. Notably, decimal maps to PostgreSQL `DECIMAL`, SQL Server `DECIMAL(18,2)`, and SQLite `REAL`; unsigned 64-bit values map to `NUMERIC(20,0)` / `DECIMAL(20,0)` / `INTEGER`. Review precision and range requirements instead of assuming lossless interchange across providers.

SQLite `GuidColumn` declares `BLOB`, while [AsGuid conversion](./guid-conversion.md) sends strings. These are separate contracts; inspect migrations and test existing-data compatibility.

## Usage examples

A complete migration class (associate it with your application's context in the usual EF migration setup):

```csharp
using Cratis.Arc.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;

public class CreateStores : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Stores",
            columns: table => new
            {
                Id = table.AutoIncrementColumn(migrationBuilder),
                Name = table.StringColumn(migrationBuilder, maxLength: 100, nullable: false),
                IsOpen = table.BoolColumn(migrationBuilder, nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_Stores", store => store.Id));
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("Stores");
    }
}
```

The helper selects types using `migrationBuilder.ActiveProvider`. Test the generated SQL and model alignment for every provider you deploy. For geometry, choose a [storage path](./point-conversion.md#choose-one-storage-path) before writing a migration.
