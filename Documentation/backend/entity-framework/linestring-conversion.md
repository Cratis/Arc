---
title: LineString conversion
description: Explicitly convert a route to a JSON string in an EF model.
---

A route contains an ordered sequence of points. `AsLineString()` stores that sequence using plain JSON string conversion; `BaseDbContext` does not apply it automatically.

## Define and configure a route

Model declaration:

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

A construction fragment for use inside a method:

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

This is **not GeoJSON**. The type and converter do not enforce the minimum two points needed for a valid geographic line. Validate the sequence before persisting it.

## Storage and queries

`AsLineString()` sets a value converter, not a SQL column type, spatial index, or structural value comparer. Do not assume record equality on coordinate arrays compares their contents, or that nested-coordinate LINQ expressions translate to SQL. Test any equality/change-tracking behavior your application depends on.

There is no `LineStringColumn()` helper for `CreateTable`. The separate `AddLineStringColumn()` migration helper declares `geometry(LineString, 4326)` on PostgreSQL, `geography` on SQL Server, and `TEXT` on SQLite. It does not make the string converter compatible with native spatial columns.

Use the [storage-path matrix](./point-conversion.md#choose-one-storage-path) to choose between explicit string conversion, `[Json]`, and application-configured native spatial mapping. Continue with [Polygon conversion](./polygon-conversion.md) for areas.
