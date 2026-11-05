---
title: Entity Framework Core Point conversion
description: Store a Cratis Point with AsPoint() in Entity Framework Core, and choose between plain JSON strings, JSON properties, and spatial columns.
---

Use Arc's `AsPoint()` to store a `Cratis.Geospatial.Point` location as a plain JSON string in Entity Framework Core. Configure it explicitly; `BaseDbContext` does not call it automatically. If you need native spatial queries instead, choose a compatible provider mapping before creating the column.

## Configure string conversion

Define the entity and context below, then register the context in your [configured Arc host](./getting-started.md). This example derives from plain `DbContext`, so Arc's automatic context discovery will not register it. The explicit registration follows the declarations; apply your database schema separately.

```csharp
using Cratis.Arc.EntityFrameworkCore;
using Cratis.Geospatial;
using Microsoft.EntityFrameworkCore;

public class Store
{
    public int Id { get; set; }
    public required Point Location { get; set; }
}

public class StoreDbContext(DbContextOptions<StoreDbContext> options) : DbContext(options)
{
    public DbSet<Store> Stores => Set<Store>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Store>().Property(store => store.Location).AsPoint();
        base.OnModelCreating(modelBuilder);
    }
}
```

After declaring the context, add this startup fragment, where `services` is the host's `IServiceCollection`:

```csharp
using Cratis.Arc.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

services.AddDbContextWithConnectionString<StoreDbContext>("Data Source=store.db");
```

`AsPoint()` takes no database argument. It uses plain `System.Text.Json`, not the GeoJSON format used for geographic data. For `new Point(Longitude: -122.4194, Latitude: 37.7749)`, the converted value is:

```json
{"Longitude":-122.4194,"Latitude":37.7749}
```

The converter does not validate coordinate ranges. Validate longitude/latitude in your application. EF normally handles a null property without passing it through the value converter; choose property and column nullability consistently.

## Choose one storage path

| Path | Configuration | Storage contract |
| --- | --- | --- |
| String conversion | Explicit `AsPoint()`, `AsLineString()`, or `AsPolygon()` | Plain JSON string; SQL type comes from the EF string mapping or your explicit configuration |
| JSON property | `[Json]` with `BaseDbContext` or `ApplyJsonConversion` | JSON conversion options and provider-specific JSON column mapping; see [JSON conversion](./json.md) |
| Spatial migration | `AddPointColumn`, `AddLineStringColumn`, `AddPolygonColumn` | Spatial SQL type declarations on PostgreSQL/SQL Server, text on SQLite; not a geometry value converter |

Choose one conversion per property: do not apply both string and `[Json]` conversion. Their serializer options and stored formats are separate. Neither conversion alone provides SQL translation for distance, intersection, or nested-coordinate LINQ queries.

## Migration usage

For a string-converted point, use an ordinary string column aligned with your model mapping. Arc has no `PointColumn()` helper for `CreateTable`. The separate `MigrationBuilder.AddPointColumn` method selects these types:

| PostgreSQL | SQL Server | SQLite |
| --- | --- | --- |
| `geometry(Point, 4326)` | `geography` | `TEXT` |

`AddPointColumn` declares a column; it does not configure native spatial conversion. Arc provides no automatic adapter between its PostgreSQL/SQL Server spatial columns and `AsPoint()`'s JSON string. For native spatial storage, configure a compatible provider mapping and database prerequisites, such as PostGIS for PostgreSQL, then verify migrations and save/load behavior for your application.

See [adding columns](./migrations-add-columns.md), [LineString conversion](./linestring-conversion.md), and [Polygon conversion](./polygon-conversion.md).
