---
title: Entity Framework Core LineString conversion
description: Store a Cratis LineString as a plain JSON string with AsLineString(), keeping route validation and native spatial mapping separate.
---

Use `AsLineString()` to store a route's ordered points as a plain JSON string in Entity Framework Core. Configure the property explicitly: `BaseDbContext` does not apply this converter automatically, and it does not provide native spatial mapping.

## Define and configure a route

Define a route with a `Cratis.Geospatial.LineString` property:

```csharp
using Cratis.Geospatial;

public class DeliveryRoute
{
    public int Id { get; set; }
    public required LineString Path { get; set; }
}
```

In your context's `OnModelCreating`, with `Cratis.Arc.EntityFrameworkCore` and `Microsoft.EntityFrameworkCore` imported:

```csharp
modelBuilder.Entity<DeliveryRoute>().Property(route => route.Path).AsLineString();
```

Inside a method, construct the route from points in longitude, latitude order:

```csharp
var route = new LineString([
    new Point(-122.4194, 37.7749),
    new Point(-122.4185, 37.7750)
]);
```

The converter's plain `System.Text.Json` output is:

```json
{"Coordinates":[{"Longitude":-122.4194,"Latitude":37.7749},{"Longitude":-122.4185,"Latitude":37.775}]}
```

This is not GeoJSON, the geographic JSON format that uses a geometry type and numeric coordinate arrays. Neither the `LineString` record nor its string converter enforces a minimum of two points. Validate the sequence before persisting it.

## Storage and queries

`AsLineString()` sets a value converter; it does not select a SQL column type or create a spatial index. It also does not add a structural value comparer to compare coordinate-array contents. Test the equality and change-tracking behavior your application depends on, especially when modifying an array in place. String conversion alone does not translate nested-coordinate LINQ expressions into SQL.

There is no `LineStringColumn()` helper for `CreateTable`. The separate `AddLineStringColumn()` migration helper declares `geometry(LineString, 4326)` on PostgreSQL, `geography` on SQL Server, and `TEXT` on SQLite. It does not make the string converter compatible with native spatial columns.

Use the [storage-path matrix](./point-conversion.md#choose-one-storage-path) to choose between explicit string conversion, `[Json]`, and application-configured native spatial mapping. Continue with [Polygon conversion](./polygon-conversion.md) for areas.
