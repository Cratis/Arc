# Common Column Types

One of the key design goals of Cratis Arc support for Entity Framework is to make it easy to support
different databases for an application.

If you're willing to hand-roll Entity Framework migrations, you can leverage the extension methods that will give you
a single migration but support different database types resolved at runtime.

## Available Column Extension Methods

The following table shows all the column extension methods available in Cratis Arc:

| Extension Method | Description | Supported Types | Parameters |
|------------------|-------------|-----------------|------------|
| `StringColumn()` | Creates a string column with appropriate database-specific type (VARCHAR/NVARCHAR/TEXT) | string | `maxLength` (int?, optional), `nullable` (bool, default: true) |
| `NumberColumn<T>()` | Creates a numeric column with appropriate database-specific type for any numeric type | char, byte, sbyte, short, ushort, int, uint, long, ulong, float, double, decimal | `nullable` (bool, default: true) |
| `BoolColumn()` | Creates a boolean column with appropriate database-specific type (BOOLEAN/BIT/INTEGER) | bool | `nullable` (bool, default: true) |
| `AutoIncrementColumn()` | Creates an auto-incrementing integer column with appropriate database-specific annotations | int | None (always non-nullable) |
| `GuidColumn()` | Creates a GUID/UUID column with appropriate database-specific type (UUID/UNIQUEIDENTIFIER/TEXT) | Guid | `nullable` (bool, default: true) |
| `DateTimeOffsetColumn()` | Creates a DateTimeOffset column with appropriate database-specific type (TIMESTAMPTZ/DATETIMEOFFSET/TEXT) | DateTimeOffset | `nullable` (bool, default: true) |
| `CoordinateColumn()` | Creates a Coordinate column with appropriate database-specific type (jsonb/nvarchar(max)/text) for geospatial data | Coordinate | `nullable` (bool, default: true) |
| `JsonColumn<T>()` | Creates a JSON column with appropriate database-specific type (jsonb/nvarchar(max)/text) | Any type | None (always non-nullable) |

## Database-Specific Type Mappings

The extension methods automatically select the appropriate SQL type based on the database provider:

### String Types

- **PostgreSQL**: `VARCHAR(n)` for limited length, `TEXT` for unlimited
- **SQL Server**: `NVARCHAR(n)` for limited length, `NVARCHAR(MAX)` for unlimited  
- **SQLite**: `TEXT`

### Numeric Types

The `NumberColumn<T>()` method supports all .NET numeric types and maps them appropriately:

- **PostgreSQL**: SMALLINT, INTEGER, BIGINT, NUMERIC, REAL, DOUBLE PRECISION, DECIMAL
- **SQL Server**: TINYINT, SMALLINT, INT, BIGINT, DECIMAL, REAL, FLOAT
- **SQLite**: INTEGER, REAL (simplified type system)

### Boolean Types

- **PostgreSQL**: `BOOLEAN`
- **SQL Server**: `BIT`
- **SQLite**: `INTEGER`

### GUID/UUID Types

- **PostgreSQL**: `UUID`
- **SQL Server**: `UNIQUEIDENTIFIER`
- **SQLite**: `TEXT`

### DateTime Types

- **PostgreSQL**: `TIMESTAMPTZ`
- **SQL Server**: `DATETIMEOFFSET`
- **SQLite**: `TEXT`

### Point Types

- **PostgreSQL**: `jsonb`
- **SQL Server**: `nvarchar(max)`
- **SQLite**: `text`

> Note: Read more about Point in [this article](./point-conversion.md)

### JSON Types

- **PostgreSQL**: `jsonb`
- **SQL Server**: `nvarchar(max)`
- **SQLite**: `text`

> Note: Read more about JSON in [this article](./json.md)

## Usage Examples

### Auto Incremental Primary Key

Auto incremental primary keys are very common but implemented in different ways with different annotations for them to
actual auto increment. The `.AutoIncrementColumn()` extension method for the `ColumnsBuilder` gives you a way to configure
it once.

```csharp
[DbContext(typeof(EventLogDbContext))]
[Migration($"EventLog_{nameof(v1_0_0)}")]
public class v1_0_0 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "EventLog",
            columns: table => new
            {
                SequenceNumber = table.AutoIncrementColumn(migrationBuilder), // Creates an auto increment column
                /*
                Other columns...
                */
            },
            constraints: table => table.PrimaryKey("PK_EventLog", x => x.SequenceNumber));
    }
}
```

### String Columns with Length Limits

```csharp
columns: table => new
{
    Name = table.StringColumn(migrationBuilder, maxLength: 100), // VARCHAR(100)/NVARCHAR(100)
    Description = table.StringColumn(migrationBuilder), // TEXT/NVARCHAR(MAX)
}
```

### Numeric Columns

```csharp
columns: table => new
{
    Count = table.NumberColumn<int>(migrationBuilder), // INTEGER/INT
    Price = table.NumberColumn<decimal>(migrationBuilder), // DECIMAL
    Score = table.NumberColumn<double>(migrationBuilder), // DOUBLE PRECISION/FLOAT/REAL
}
```

### Other Column Types

```csharp
columns: table => new
{
    IsActive = table.BoolColumn(migrationBuilder), // BOOLEAN/BIT/INTEGER
    Id = table.GuidColumn(migrationBuilder, nullable: false), // UUID/UNIQUEIDENTIFIER/TEXT
    CreatedAt = table.DateTimeOffsetColumn(migrationBuilder), // TIMESTAMPTZ/DATETIMEOFFSET/TEXT
    Metadata = table.JsonColumn<Dictionary<string, object>>(migrationBuilder), // jsonb/nvarchar(max)/text
}
```
