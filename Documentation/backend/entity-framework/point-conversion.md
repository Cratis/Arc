---
title: Point conversion
description: Store a Cratis Point explicitly without confusing JSON conversion and spatial column types.
---

To store a location, first choose its representation. Arc's `AsPoint()` converts a `Cratis.Geospatial.Point` to a JSON **string**; it does not install a native spatial provider or select a JSON SQL column type. `BaseDbContext` does not call it automatically.

## Configure string conversion

These are complete model/context declarations. This example derives from plain `DbContext`, which Arc's automatic discovery does not discover. Register it explicitly in your [configured Arc host](./getting-started.md) and apply your schema separately.

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

`AsPoint()` takes no database argument. It uses plain `System.Text.Json`, not MongoDB's GeoJSON serializer. For `new Point(Longitude: -122.4194, Latitude: 37.7749)`, the converted value is:

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

Do not apply both string and `[Json]` conversion to the same property. Their serializer options are separate. Neither path promises translated distance, intersection, or nested-coordinate LINQ queries.

## Migration usage

Arc has **no `PointColumn()` helper for `CreateTable`**. A string-converted point can use an ordinary string column, aligned with your model mapping. The separate `MigrationBuilder.AddPointColumn` method selects:

| PostgreSQL | SQL Server | SQLite |
| --- | --- | --- |
| `geometry(Point, 4326)` | `geography` | `TEXT` |

This is a schema helper, **not a complete native spatial recipe**. Pairing its PostgreSQL/SQL Server columns with `AsPoint()` is not supported by an automatic adapter. Native spatial usage requires a compatible provider mapping, database prerequisites (such as PostGIS), migrations, and round-trip tests designed by your application.

See [adding columns](./migrations-add-columns.md), [LineString conversion](./linestring-conversion.md), and [Polygon conversion](./polygon-conversion.md).
