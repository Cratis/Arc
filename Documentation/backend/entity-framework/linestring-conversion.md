# LineString Conversion

The LineString conversion feature provides automatic handling of `LineString` geospatial properties from Cratis.Fundamentals in Entity Framework Core, enabling storage of routes, paths, and other linear geographic features.

## What it does

The LineString conversion automatically configures Entity Framework Core to handle properties of type `LineString` using JSON serialization:

1. **PostgreSQL**: Stores as `jsonb` type for efficient JSON queries
2. **SQL Server**: Stores as `nvarchar(max)` with JSON serialization
3. **SQLite**: Stores as `text` with JSON serialization

This ensures LineStrings (ordered sequences of geographic points) are stored consistently across all database providers.

## Why it's important

Using LineString conversion provides several key benefits:

- **Route and Path Support**: Store delivery routes, walking paths, or any linear geographic features
- **Cross-Database Compatibility**: Consistent LineString handling across different database providers
- **JSON Serialization**: Uses standard JSON format for storage, making data human-readable
- **Type Safety**: Maintains strong typing with the `LineString` type from Cratis.Fundamentals
- **Automatic Configuration**: No need for manual configuration of LineString properties

## The LineString Type

The `LineString` type represents an ordered sequence of connected geographic points:

```csharp
using Cratis.Geospatial;

var points = new Point[]
{
    new Point(longitude: -122.4194, latitude: 37.7749),
    new Point(longitude: -122.4185, latitude: 37.7750),
    new Point(longitude: -122.4170, latitude: 37.7755)
};

var route = new LineString(points);
```

The LineString is serialized to JSON as:

```json
{
  "coordinates": [
    [-122.4194, 37.7749],
    [-122.4185, 37.7750],
    [-122.4170, 37.7755]
  ]
}
```

## Model Usage

Your entity models can use `LineString` properties directly:

```csharp
using Cratis.Geospatial;

public class DeliveryRoute
{
    public Guid Id { get; set; }
    public string RouteName { get; set; }
    public LineString Path { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class UserTrack
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public LineString TrackingPath { get; set; }
    public LineString? HistoricalPath { get; set; } // Nullable LineString
}
```

The conversion will automatically:

- Configure all `LineString` properties to use JSON serialization
- Store the ordered points as a JSON array in the database
- Handle conversion between .NET `LineString` instances and JSON strings

## Manual Configuration

If you're not using the [`BaseDbContext`](./base-db-context.md), you can manually apply LineString conversion:

```csharp
using Cratis.Arc.EntityFrameworkCore;
using Cratis.Geospatial;

public class RouteDbContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<DeliveryRoute> Routes { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DeliveryRoute>(entity =>
        {
            entity.Property(e => e.Path)
                .AsLineString();
        });
        
        base.OnModelCreating(modelBuilder);
    }
}
```

> Note: This is automatically configured for you when using the [`BaseDbContext`](./base-db-context.md).

## Migration Usage

When creating migrations, use the `LineStringColumn()` extension method:

### Creating a Table with LineString Column

```csharp
[DbContext(typeof(RouteDbContext))]
[Migration($"Routes_{nameof(v1_0_0)}")]
public class v1_0_0 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "DeliveryRoutes",
            columns: table => new
            {
                Id = table.GuidColumn(migrationBuilder),
                RouteName = table.StringColumn(migrationBuilder, maxLength: 200, nullable: false),
                Path = table.LineStringColumn(migrationBuilder, nullable: false),
                CreatedAt = table.DateTimeOffsetColumn(migrationBuilder, nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_Routes", x => x.Id));
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "DeliveryRoutes");
    }
}
```

### Adding a LineString Column to Existing Table

```csharp
[DbContext(typeof(RouteDbContext))]
[Migration($"Routes_{nameof(v1_1_0)}")]
public class v1_1_0 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddLineStringColumn(
            name: "OptimizedPath",
            table: "DeliveryRoutes",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "OptimizedPath",
            table: "DeliveryRoutes");
    }
}
```

> See [Common Column Types](./common-column-types.md) for more information about column type extensions.

## How it works

The conversion system uses a `ValueConverter` that:

1. **Serializes**: Converts `LineString` instances to JSON strings using `System.Text.Json`
2. **Deserializes**: Parses JSON strings back to `LineString` instances when reading from database
3. **Null handling**: Properly handles nullable `LineString?` properties
4. **Validation**: Ensures LineStrings contain at least 2 points

The conversion is handled by the `AsLineString()` extension method, which configures the property with the appropriate `ValueConverter`.

## Database Provider Specifics

The LineString conversion adapts to different database providers:

- **PostgreSQL (Npgsql)**: Uses `jsonb` type for efficient JSON queries
- **SQL Server**: Uses `nvarchar(max)` with JSON string storage
- **SQLite**: Uses `text` with JSON string storage

## Querying Considerations

When querying LineString data:

### All Providers

For general LineString comparison:

```csharp
// Exact match
var routes = await context.Routes
    .Where(r => r.Path == targetPath)
    .ToListAsync();

// Null checks
var routesWithPath = await context.Routes
    .Where(r => r.Path != null)
    .ToListAsync();
```

> Note: For advanced geospatial queries (distance calculations along path, intersection detection, etc.), consider using database-specific extensions or computing results in application code.

## Related Topics

- [Common Column Types](./common-column-types.md) - Column type extensions
- [Property Extensions](./property-extensions.md) - AsLineString() and other configuration methods
- [Polygon Conversion](./polygon-conversion.md) - Working with geographic areas
- [Point Conversion](./point-conversion.md) - Working with individual points
- [JSON Conversion](./json.md) - General JSON serialization support
