# Coordinate Conversion

The Coordinate conversion feature provides automatic handling of `Coordinate` geospatial properties from Cratis.Fundamentals in Entity Framework Core, ensuring consistent storage and optimal database compatibility across different database providers.

## What it does

The Coordinate conversion automatically configures Entity Framework Core to handle properties of type `Coordinate` using the most appropriate database representation for each provider:

1. **PostgreSQL**: Stores as `jsonb` type for efficient JSON queries and optimal storage
2. **SQL Server**: Stores as `nvarchar(max)` with JSON serialization
3. **SQLite**: Stores as `text` with JSON serialization

This automatic configuration ensures that Coordinates (longitude/latitude pairs) are stored in a consistent format across all database providers while maintaining compatibility and optimal performance for each database.

## Why it's important

Using Coordinate conversion provides several key benefits:

- **Geospatial Support**: Built-in support for storing location data (longitude/latitude pairs)
- **Cross-Database Compatibility**: Consistent Coordinate handling across different database providers
- **JSON Serialization**: Uses standard JSON format for storage, making data human-readable and queryable
- **Type Safety**: Maintains strong typing with the `Coordinate` type from Cratis.Fundamentals
- **Automatic Configuration**: No need for manual configuration of Coordinate properties

## The Coordinate Type

The `Coordinate` type from Cratis.Fundamentals represents a geographic coordinate with longitude and latitude:

```csharp
using Cratis.Geospatial;

var coordinate = new Coordinate(longitude: -122.4194, latitude: 37.7749); // San Francisco
```

The coordinate is serialized to JSON as:

```json
{
  "longitude": -122.4194,
  "latitude": 37.7749
}
```

## Model Usage

Your entity models can use `Coordinate` properties directly without any special configuration when using the [`BaseDbContext`](./base-db-context.md):

```csharp
using Cratis.Geospatial;

public class Store
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public Coordinate Location { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class DeliveryPoint
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Coordinate Destination { get; set; }
    public Coordinate? CurrentLocation { get; set; } // Nullable coordinate
}
```

The conversion will automatically:

- Configure all `Coordinate` properties to use JSON serialization for storage
- Store the longitude and latitude as a JSON object in the database
- Handle conversion between .NET `Coordinate` instances and JSON strings

## Manual Configuration

If you're not using the [`BaseDbContext`](./base-db-context.md), you can manually apply Coordinate conversion in your `DbContext`:

```csharp
using Cratis.Arc.EntityFrameworkCore;
using Cratis.Geospatial;

public class StoreDbContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<Store> Stores { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Store>(entity =>
        {
            entity.Property(e => e.Location)
                .AsCoordinate(Database.GetDatabaseType());
        });
        
        base.OnModelCreating(modelBuilder);
    }
}
```

> Note: This is automatically configured for you when using the [`BaseDbContext`](./base-db-context.md).

## Migration Usage

When creating migrations, use the `CoordinateColumn()` extension method for creating Coordinate columns:

### Creating a Table with Coordinate Column

```csharp
[DbContext(typeof(StoreDbContext))]
[Migration($"Stores_{nameof(v1_0_0)}")]
public class v1_0_0 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Stores",
            columns: table => new
            {
                Id = table.GuidColumn(migrationBuilder),
                Name = table.StringColumn(migrationBuilder, maxLength: 200, nullable: false),
                Location = table.CoordinateColumn(migrationBuilder, nullable: false),
                CreatedAt = table.DateTimeOffsetColumn(migrationBuilder, nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_Stores", x => x.Id));
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "Stores");
    }
}
```

### Adding a Coordinate Column to Existing Table

```csharp
[DbContext(typeof(StoreDbContext))]
[Migration($"Stores_{nameof(v1_1_0)}")]
public class v1_1_0 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddCoordinateColumn(
            name: "WarehouseLocation",
            table: "Stores",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "WarehouseLocation",
            table: "Stores");
    }
}
```

> See [Common Column Types](./common-column-types.md) for more information about column type extensions.

## How it works

The conversion system uses a `ValueConverter` that:

1. **Serializes**: Converts `Coordinate` instances to JSON strings using `System.Text.Json`
2. **Deserializes**: Parses JSON strings back to `Coordinate` instances when reading from database
3. **Null handling**: Properly handles nullable `Coordinate?` properties

The conversion is handled by the `AsCoordinate()` extension method, which configures the property with the appropriate `ValueConverter`.

## Database Provider Specifics

The Coordinate conversion adapts to different database providers:

- **PostgreSQL (Npgsql)**: Uses `jsonb` type for efficient JSON queries and indexing capabilities
- **SQL Server**: Uses `nvarchar(max)` with JSON string storage
- **SQLite**: Uses `text` with JSON string storage

This provider-specific optimization ensures the best storage characteristics for your chosen database, with PostgreSQL gaining the additional benefit of being able to query within the JSON structure.

## Querying Considerations

When querying Coordinate data:

### PostgreSQL

PostgreSQL's `jsonb` type allows for efficient queries within the JSON structure:

```csharp
// You can still query for stores with specific coordinates
var stores = await context.Stores
    .Where(s => s.Location == targetCoordinate)
    .ToListAsync();
```

### All Providers

For general coordinate comparison across all providers:

```csharp
// Exact match
var store = await context.Stores
    .FirstOrDefaultAsync(s => s.Location == knownLocation);

// Null checks work as expected
var storesWithLocation = await context.Stores
    .Where(s => s.Location != null)
    .ToListAsync();
```

> Note: For advanced geospatial queries (distance calculations, radius searches, etc.), consider using database-specific extensions or computing distances in application code after retrieving the data.

## Related Topics

- [Common Column Types](./common-column-types.md) - Column type extensions including CoordinateColumn()
- [Property Extensions](./property-extensions.md) - AsCoordinate() and other property configuration methods
- [JSON Conversion](./json.md) - General JSON serialization support in Entity Framework Core
- [Adding Columns in Migrations](./migrations-add-columns.md) - AddCoordinateColumn() method
