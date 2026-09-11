---
title: Add columns in migrations
description: Reference for Arc migration column helpers and provider-specific declarations.
---

Arc's column helpers let a migration select the corresponding database type for PostgreSQL, SQL Server, or SQLite. The mappings below show where those providers differ.

These extension methods are similar to the [Common Column Types](./common-column-types.md) used when creating tables, but are specifically designed for `AddColumn` operations in migrations.

## Available AddColumn Extension Methods

The following methods extend `MigrationBuilder`. Import `Cratis.Arc.EntityFrameworkCore`; `AddJsonColumn<T>` additionally requires `Cratis.Arc.EntityFrameworkCore.Json`. Examples are migration fragments using your application's context and schema; they do not run independently.

| Extension Method            | Description                                                                                            | Supported Types                                                                  | Parameters                                                                                                                                        |
| --------------------------- | ------------------------------------------------------------------------------------------------------ | -------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------- |
| `AddStringColumn()`         | Adds a string column with appropriate database-specific type (VARCHAR/NVARCHAR/TEXT)                   | string                                                                           | `name`, `table`, `maxLength` (int?, optional), `nullable` (bool, default: true), `defaultValue` (string?, optional), `schema` (string?, optional) |
| `AddNumberColumn<T>()`      | Adds a numeric column with appropriate database-specific type for any numeric type                     | char, byte, sbyte, short, ushort, int, uint, long, ulong, float, double, decimal | `name`, `table`, `nullable` (bool, default: true), `defaultValue` (object?, optional), `schema` (string?, optional)                               |
| `AddBoolColumn()`           | Adds a boolean column with appropriate database-specific type (BOOLEAN/BIT/INTEGER)                    | bool                                                                             | `name`, `table`, `nullable` (bool, default: true), `defaultValue` (bool, default: false), `schema` (string?, optional)                            |
| `AddAutoIncrementColumn()`  | Adds an auto-incrementing integer column with appropriate database-specific annotations                | int                                                                              | `name`, `table`, `schema` (string?, optional)                                                                                                     |
| `AddGuidColumn()`           | Adds a GUID/UUID column with appropriate database-specific type (UUID/UNIQUEIDENTIFIER/BLOB)           | Guid                                                                             | `name`, `table`, `nullable` (bool, default: true), `schema` (string?, optional)                                                                   |
| `AddDateTimeOffsetColumn()` | Adds a DateTimeOffset column with appropriate database-specific type (TIMESTAMPTZ/DATETIMEOFFSET/TEXT) | DateTimeOffset                                                                   | `name`, `table`, `nullable` (bool, default: true), `schema` (string?, optional)                                                                   |
| `AddPointColumn()`          | Declares a spatial/text column; not a model converter                                                  | Point                                                                            | `name`, `table`, `nullable: true`, `schema`                                                                                                       |
| `AddLineStringColumn()`     | Declares a spatial/text column; not a model converter                                                  | LineString                                                                       | `name`, `table`, `nullable: true`, `schema`                                                                                                       |
| `AddPolygonColumn()`        | Declares a spatial/text column; not a model converter                                                  | Polygon                                                                          | `name`, `table`, `nullable: true`, `schema`                                                                                                       |
| `AddJsonColumn<T>()`        | Declares a non-nullable JSON column                                                                    | JSON property type                                                               | `name`, `table`, `schema`                                                                                                                         |

Geometry helpers select `geometry(Point, 4326)`, `geometry(LineString, 4326)`, or `geometry(Polygon, 4326)` on PostgreSQL; `geography` on SQL Server; and `TEXT` on SQLite. They do not install spatial provider mappings or make plain JSON string converters compatible with native spatial columns. See the [storage-path matrix](./point-conversion.md#choose-one-storage-path).

Adding non-nullable columns to populated tables may require a default or a staged backfill. Verify the generated SQL, model compatibility, permissions, and provider restrictions before applying any migration.

## Database-Specific Type Mappings

The extension methods automatically select the appropriate SQL type based on the database provider. See [Common Column Types](./common-column-types.md) for detailed type mappings for each database.

## Usage Examples

### Adding String Columns

```csharp
[DbContext(typeof(MyDbContext))]
[Migration($"MyContext_{nameof(v1_1_0)}")]
public class v1_1_0 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Add string column with length limit
        migrationBuilder.AddStringColumn(
            name: "Email",
            table: "Users",
            maxLength: 255,
            nullable: false);

        // Add unlimited text column with default value
        migrationBuilder.AddStringColumn(
            name: "Description",
            table: "Products",
            defaultValue: "No description provided");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "Email", table: "Users");
        migrationBuilder.DropColumn(name: "Description", table: "Products");
    }
}
```

### Adding Numeric Columns

```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    // Add integer column
    migrationBuilder.AddNumberColumn<int>(
        name: "ViewCount",
        table: "Articles",
        defaultValue: 0,
        nullable: false);

    // Add decimal column for currency
    migrationBuilder.AddNumberColumn<decimal>(
        name: "Price",
        table: "Products",
        nullable: true);

    // Add double column for ratings
    migrationBuilder.AddNumberColumn<double>(
        name: "Rating",
        table: "Reviews",
        defaultValue: 0.0,
        nullable: false);
}
```

### Adding Boolean Columns

```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.AddBoolColumn(
        name: "IsActive",
        table: "Users",
        defaultValue: true,
        nullable: false);

    migrationBuilder.AddBoolColumn(
        name: "IsDeleted",
        table: "Orders",
        defaultValue: false);
}
```

### Adding GUID Columns

```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.AddGuidColumn(
        name: "ExternalId",
        table: "Users",
        nullable: false);

    migrationBuilder.AddGuidColumn(
        name: "CorrelationId",
        table: "Events",
        nullable: true);
}
```

### Adding DateTime Columns

```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.AddDateTimeOffsetColumn(
        name: "LastLoginAt",
        table: "Users",
        nullable: true);

    migrationBuilder.AddDateTimeOffsetColumn(
        name: "CreatedAt",
        table: "Orders",
        nullable: false);
}
```

### Adding Auto-Increment Columns

```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    // Add auto-increment column (useful for adding sequence numbers to existing tables)
    migrationBuilder.AddAutoIncrementColumn(
        name: "SequenceNumber",
        table: "AuditLog");
}
```

### Using Schemas

For databases that support schemas (PostgreSQL, SQL Server), you can specify the schema:

```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.AddStringColumn(
        name: "Department",
        table: "Employees",
        schema: "hr",
        maxLength: 100,
        nullable: false);
}
```

## Benefits

Using these extension methods provides several advantages:

- **Database Portability**: Write migrations once and support multiple database providers
- **Type Safety**: Strongly typed methods reduce errors
- **Consistency**: Ensures consistent type mapping across your application
- **Simplified Syntax**: Less boilerplate code compared to manually specifying types
- **Maintainability**: Centralized type mapping logic makes updates easier

## See Also

- [Common Column Types](./common-column-types.md) - For creating columns when defining new tables
- [Base DbContext](./base-db-context.md) - Learn about the base DbContext implementation
- [Automatic Database Hookup](./automatic-database-hookup.md) - Automatic database configuration
