---
title: Polygon conversion
description: Explicitly convert geographic boundaries to JSON strings and understand validation limits.
---

A polygon represents an exterior boundary and optional holes. Arc's `AsPolygon()` converts the record to a plain JSON string. It is not automatically applied by `BaseDbContext` and is not native spatial mapping.

## Define and configure an area

Model declaration:

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

Construction fragment inside a method:

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

Plain JSON conversion writes `Shell` and `Holes`; rings contain `Coordinates` arrays of objects with `Longitude` and `Latitude`. It does not write GeoJSON's `type` and nested numeric coordinate arrays.

## Validate boundaries yourself

Neither the Fundamentals records nor `AsPolygon()` enforce ring closure, minimum point count, winding, or freedom from self-intersection. Your application must validate the geometry it requires. Use closed rings with at least three distinct vertices and a repeated closing point when targeting GeoJSON-compatible storage.

The converter does not add a structural value comparer. Replace values deliberately and test change tracking when arrays or rings are modified. Do not promise SQL translation for containment or intersection from string conversion alone.

## Migrations

There is no `PolygonColumn()` helper for `CreateTable`. The distinct `AddPolygonColumn()` helper selects `geometry(Polygon, 4326)` on PostgreSQL, `geography` on SQL Server, and `TEXT` on SQLite. Those declarations do not install a compatible value converter or database spatial extension.

Choose a representation using the [storage-path matrix](./point-conversion.md#choose-one-storage-path). See [JSON conversion](./json.md) for `[Json]` properties and [adding columns](./migrations-add-columns.md) for migration helpers.
