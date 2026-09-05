# Polygon Conversion

The Polygon conversion feature provides automatic handling of `Polygon` geospatial properties from Cratis.Fundamentals in Entity Framework Core, enabling storage of complex geographic areas with optional interior boundaries (holes).

## What it does

The Polygon conversion automatically configures Entity Framework Core to handle properties of type `Polygon` using JSON serialization:

1. **PostgreSQL**: Stores as `jsonb` type for efficient JSON queries
2. **SQL Server**: Stores as `nvarchar(max)` with JSON serialization
3. **SQLite**: Stores as `text` with JSON serialization

This ensures Polygons (closed geographic shapes with optional holes) are stored consistently across all database providers.

## Why it's important

Using Polygon conversion provides several key benefits:

- **Geographic Areas**: Store service areas, regions, or any bounded geographic territory
- **Complex Boundaries**: Support for interior holes (e.g., a region with an excluded zone)
- **Cross-Database Compatibility**: Consistent Polygon handling across different database providers
- **JSON Serialization**: Uses standard JSON format for storage, making data human-readable
- **Type Safety**: Maintains strong typing with the `Polygon` type from Cratis.Fundamentals
- **Automatic Configuration**: No need for manual configuration of Polygon properties

## The Polygon Type

The `Polygon` type represents a closed geographic shape with a required exterior boundary (shell) and optional interior boundaries (holes):

```csharp
using Cratis.Geospatial;

// Simple polygon (no holes)
var shellPoints = new Point[]
{
    new Point(longitude: -122.4194, latitude: 37.7749),
    new Point(longitude: -122.4170, latitude: 37.7755),
    new Point(longitude: -122.4160, latitude: 37.7740),
    new Point(longitude: -122.4194, latitude: 37.7749) // Must close the ring
};

var simplePolygon = new Polygon(
    new LinearRing(shellPoints),
    holes: []
);

// Polygon with a hole
var holePoints = new Point[]
{
    new Point(longitude: -122.4180, latitude: 37.7750),
    new Point(longitude: -122.4175, latitude: 37.7752),
    new Point(longitude: -122.4170, latitude: 37.7748),
    new Point(longitude: -122.4180, latitude: 37.7750) // Must close the ring
};

var polygonWithHole = new Polygon(
    new LinearRing(shellPoints),
    holes: [new LinearRing(holePoints)]
);
```

The Polygon is serialized to JSON as:

```json
{
  "shell": {
    "coordinates": [
      [-122.4194, 37.7749],
      [-122.4170, 37.7755],
      [-122.4160, 37.7740],
      [-122.4194, 37.7749]
    ]
  },
  "holes": []
}
```

## Model Usage

Your entity models can use `Polygon` properties directly:

```csharp
using Cratis.Geospatial;

public class ServiceArea
{
    public Guid Id { get; set; }
    public string AreaName { get; set; }
    public Polygon Boundary { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ExclusionZone
{
    public Guid Id { get; set; }
    public Guid ServiceAreaId { get; set; }
    public Polygon RestrictedArea { get; set; }
    public Polygon? AdditionalRestriction { get; set; } // Nullable polygon
}
```

The conversion will automatically:

- Configure all `Polygon` properties to use JSON serialization
- Store the shell and holes as a JSON structure in the database
- Handle conversion between .NET `Polygon` instances and JSON strings

## Manual Configuration

If you're not using the [`BaseDbContext`](./base-db-context.md), you can manually apply Polygon conversion:

```csharp
using Cratis.Arc.EntityFrameworkCore;
using Cratis.Geospatial;

public class ServiceAreaDbContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<ServiceArea> Areas { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ServiceArea>(entity =>
        {
            entity.Property(e => e.Boundary)
                .AsPolygon();
        });
        
        base.OnModelCreating(modelBuilder);
    }
}
```

> Note: This is automatically configured for you when using the [`BaseDbContext`](./base-db-context.md).

## Migration Usage

When creating migrations, use the `PolygonColumn()` extension method:

### Creating a Table with Polygon Column

```csharp
[DbContext(typeof(ServiceAreaDbContext))]
[Migration($"ServiceAreas_{nameof(v1_0_0)}")]
public class v1_0_0 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "ServiceAreas",
            columns: table => new
            {
                Id = table.GuidColumn(migrationBuilder),
                AreaName = table.StringColumn(migrationBuilder, maxLength: 200, nullable: false),
                Boundary = table.PolygonColumn(migrationBuilder, nullable: false),
                CreatedAt = table.DateTimeOffsetColumn(migrationBuilder, nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_ServiceAreas", x => x.Id));
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "ServiceAreas");
    }
}
```

### Adding a Polygon Column to Existing Table

```csharp
[DbContext(typeof(ServiceAreaDbContext))]
[Migration($"ServiceAreas_{nameof(v1_1_0)}")]
public class v1_1_0 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddPolygonColumn(
            name: "ExpandedBoundary",
            table: "ServiceAreas",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "ExpandedBoundary",
            table: "ServiceAreas");
    }
}
```

> See [Common Column Types](./common-column-types.md) for more information about column type extensions.

## How it works

The conversion system uses a `ValueConverter` that:

1. **Serializes**: Converts `Polygon` instances to JSON strings using `System.Text.Json`
2. **Deserializes**: Parses JSON strings back to `Polygon` instances when reading from database
3. **Null handling**: Properly handles nullable `Polygon?` properties
4. **Validation**: Ensures rings are properly closed (first and last points match) and contain at least 4 points

The conversion is handled by the `AsPolygon()` extension method, which configures the property with the appropriate `ValueConverter`.

## Polygon Requirements

When creating Polygon instances, ensure:

- **Closed rings**: Each LinearRing must have identical first and last points (the ring must close)
- **Minimum points**: Each ring must have at least 4 points (3 unique points + 1 closing duplicate)
- **Correct order**: Exterior ring (shell) vertices should follow right-hand rule for GeoJSON compatibility
- **No self-intersection**: Rings should not cross themselves

## Database Provider Specifics

The Polygon conversion adapts to different database providers:

- **PostgreSQL (Npgsql)**: Uses `jsonb` type for efficient JSON queries
- **SQL Server**: Uses `nvarchar(max)` with JSON string storage
- **SQLite**: Uses `text` with JSON string storage

## Querying Considerations

When querying Polygon data:

### All Providers

For general Polygon comparison:

```csharp
// Exact match
var areas = await context.Areas
    .Where(a => a.Boundary == targetBoundary)
    .ToListAsync();

// Null checks
var areasWithBoundary = await context.Areas
    .Where(a => a.Boundary != null)
    .ToListAsync();
```

> Note: For advanced geospatial queries (point-in-polygon, intersection detection, etc.), consider using database-specific extensions or computing results in application code.

## Related Topics

- [Common Column Types](./common-column-types.md) - Column type extensions
- [Property Extensions](./property-extensions.md) - AsPolygon() and other configuration methods
- [LineString Conversion](./linestring-conversion.md) - Working with routes and paths
- [Point Conversion](./point-conversion.md) - Working with individual points
- [JSON Conversion](./json.md) - General JSON serialization support
