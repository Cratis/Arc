---
title: Entity Framework Core Polygon conversion
description: Store a Cratis Polygon as a plain JSON string with AsPolygon(), while handling boundary validation and spatial column mapping separately.
---

Use Arc's `AsPolygon()` to store an area's exterior boundary and optional holes as a plain JSON string in Entity Framework Core. Configure the property explicitly: `BaseDbContext` does not apply this converter automatically. The string conversion does not provide native spatial mapping.

## Define and configure an area

Define an area with a `Cratis.Geospatial.Polygon` property:

```csharp
using Cratis.Geospatial;

public class ServiceArea
{
    public int Id { get; set; }
    public required Polygon Boundary { get; set; }
}
```

Inside `OnModelCreating`, with `Cratis.Arc.EntityFrameworkCore` and `Microsoft.EntityFrameworkCore` imported:

```csharp
modelBuilder.Entity<ServiceArea>().Property(area => area.Boundary).AsPolygon();
```

Inside a method, construct the boundary from a closed exterior ring. This example has no holes:

```csharp
var boundary = new Polygon(
    new LinearRing([
        new Point(0, 0),
        new Point(1, 0),
        new Point(0, 1),
        new Point(0, 0)
    ]),
    Holes: []);
```

The stored JSON has `Shell` and `Holes` properties. Each ring contains a `Coordinates` array of objects with `Longitude` and `Latitude`. This differs from GeoJSON, the geographic JSON format that uses a `type` property and nested numeric coordinate arrays.

## Validate boundaries yourself

Neither the `Cratis.Geospatial` records nor `AsPolygon()` validates that a ring closes, has enough points, runs in the required direction, or avoids crossing itself. Validate the geometry your application requires before saving it. For GeoJSON-compatible rings, use at least three distinct vertices and repeat the first point at the end; this does not turn the converter's output into GeoJSON.

The converter does not add a structural value comparer to compare the contents of arrays and rings. Replace values deliberately and verify EF change tracking when those contents change. String conversion alone does not translate containment or intersection queries into SQL.

## Migrations

There is no `PolygonColumn()` helper for `CreateTable`. The distinct `AddPolygonColumn()` helper selects `geometry(Polygon, 4326)` on PostgreSQL, `geography` on SQL Server, and `TEXT` on SQLite. Those declarations do not install a compatible value converter or database spatial extension.

Choose a representation using the [storage-path matrix](./point-conversion.md#choose-one-storage-path). See [JSON conversion](./json.md) for `[Json]` properties and [adding columns](./migrations-add-columns.md) for migration helpers.
