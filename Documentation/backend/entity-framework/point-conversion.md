# Point Conversion

The Point conversion feature provides automatic handling of `Point` geospatial properties from Cratis.Fundamentals in Entity Framework Core, ensuring consistent storage and optimal database compatibility across different database providers.

## What it does

The Point conversion automatically configures Entity Framework Core to handle properties of type `Point` using the most appropriate database representation for each provider:

1. **PostgreSQL**: Stores as `jsonb` type for efficient JSON queries and optimal storage
2. **SQL Server**: Stores as `nvarchar(max)` with JSON serialization
3. **SQLite**: Stores as `text` with JSON serialization

This automatic configuration ensures that Points (longitude/latitude pairs) are stored in a consistent format across all database providers while maintaining compatibility and optimal performance for each database.

## Why it's important

Using Point conversion provides several key benefits:

- **Geospatial Support**: Built-in support for storing location data (longitude/latitude pairs)
- **Cross-Database Compatibility**: Consistent Point handling across different database providers
- **JSON Serialization**: Uses standard JSON format for storage, making data human-readable and queryable
- **Type Safety**: Maintains strong typing with the `Point` type from Cratis.Fundamentals
- **Automatic Configuration**: No need for manual configuration of Point properties

## The Point Type

The `Point` type from Cratis.Fundamentals represents a geographic point with longitude and latitude:

```csharp
using Cratis.Geospatial;

var point = new Point(longitude: -122.4194, latitude: 37.7749); // San Francisco
```

The point is serialized to JSON as:

```json
{
  "longitude": -122.4194,
  "latitude": 37.7749
}
```

## Model Usage

Your entity models can use `Point` properties directly without any special configuration when using the [`BaseDbContext`](./base-db-context.md):

```csharp
using Cratis.Geospatial;

public class Store
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public Point Location { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class DeliveryPoint
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Point Destination { get; set; }
    public Point? CurrentLocation { get; set; } // Nullable point
}
```

The conversion will automatically:

- Configure all `Coordinate` properties to use JSON serialization for storage
- Store the longitude and latitude as a JSON object in the database
- Handle conversion between .NET `Coordinate` instances and JSON strings

## Manual Configuration

If you're not using the [`BaseDbContext`](./base-db-context.md), you can manually apply Point conversion in your `DbContext`:

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
                .AsPoint(Database.GetDatabaseType());
        });
        
        base.OnModelCreating(modelBuilder);
    }
}
```

> Note: This is automatically configured for you when using the [`BaseDbContext`](./base-db-context.md).

## Migration Usage

When creating migrations, use the `PointColumn()` extension method for creating Point columns:

### Creating a Table with Point Column

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
                Location = table.PointColumn(migrationBuilder, nullable: false),
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

### Adding a Point Column to Existing Table

```csharp
[DbContext(typeof(StoreDbContext))]
[Migration($"Stores_{nameof(v1_1_0)}")]
public class v1_1_0 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddPointColumn(
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

1. **Serializes**: Converts `Point` instances to JSON strings using `System.Text.Json`
2. **Deserializes**: Parses JSON strings back to `Point` instances when reading from database
3. **Null handling**: Properly handles nullable `Point?` properties

The conversion is handled by the `AsPoint()` extension method, which configures the property with the appropriate `ValueConverter`.

## Database Provider Specifics

The Point conversion adapts to different database providers:

- **PostgreSQL (Npgsql)**: Uses `jsonb` type for efficient JSON queries and indexing capabilities
- **SQL Server**: Uses `nvarchar(max)` with JSON string storage
- **SQLite**: Uses `text` with JSON string storage

This provider-specific optimization ensures the best storage characteristics for your chosen database, with PostgreSQL gaining the additional benefit of being able to query within the JSON structure.

## Querying Considerations

When querying Point data:

### PostgreSQL

PostgreSQL's `jsonb` type allows for efficient queries within the JSON structure:

```csharp
// You can still query for stores with specific points
var stores = await context.Stores
    .Where(s => s.Location == targetPoint)
    .ToListAsync();
```

### All Providers

For general point comparison across all providers:

```csharp
// Exact match
var store = await context.Stores
    .FirstOrDefaultAsync(s => s.Location == knownPoint);

// Null checks work as expected
var storesWithLocation = await context.Stores
    .Where(s => s.Location != null)
    .ToListAsync();
```

> Note: For advanced geospatial queries (distance calculations, radius searches, etc.), consider using database-specific extensions or computing distances in application code after retrieving the data.

## Related Topics

- [Common Column Types](./common-column-types.md) - Column type extensions including PointColumn()
- [Property Extensions](./property-extensions.md) - AsPoint() and other property configuration methods
- [JSON Conversion](./json.md) - General JSON serialization support in Entity Framework Core
- [Adding Columns in Migrations](./migrations-add-columns.md) - AddPointColumn() method
