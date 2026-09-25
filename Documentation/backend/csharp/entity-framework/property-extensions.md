---
title: Property extensions
description: Reference for Arc EF PropertyBuilder value converters.
---

Import `Cratis.Arc.EntityFrameworkCore` for these extensions on EF's `PropertyBuilder`.

## AsGuid()

`AsGuid(DatabaseType databaseType)` adds a Guid/string converter for SQLite using the `D` format and `Guid.Parse`. For plain Guid properties on PostgreSQL and SQL Server, it leaves the EF provider mapping unchanged. For concept properties it delegates to `AsConcept`.

`BaseDbContext` applies Guid conversion to relevant model types. Manual `OnModelCreating` fragment for a `Customer.Id` Guid property:

```csharp
modelBuilder.Entity<Customer>().Property(customer => customer.Id).AsGuid(Database.GetDatabaseType());
```

This does not specify `CHAR(36)`, collation, or indexes. [Guid conversion](./guid-conversion.md) explains the distinction from migration column types.

## AsConcept()

`AsConcept(DatabaseType databaseType)` unwraps `ConceptAs<T>` to its primitive value and reconstructs the concept on reads. It configures a concept value comparer and does nothing for non-concept properties. Guid concepts use string conversion for SQLite.

Manual model-configuration fragment:

```csharp
modelBuilder.Entity<Customer>().Property(customer => customer.Id).AsConcept(Database.GetDatabaseType());
```

Here `Customer.Id` is a concept property. Use records, not classes, for concept declarations, and nullable concept references for optional values. See [concept conversion](./concept-as-conversion.md). `BaseDbContext` already applies this conversion for relevant model types.

## Geometry conversions

| Method | Model type | Provider value |
| --- | --- | --- |
| `AsPoint()` | `Cratis.Geospatial.Point` | Plain JSON string |
| `AsLineString()` | `Cratis.Geospatial.LineString` | Plain JSON string |
| `AsPolygon()` | `Cratis.Geospatial.Polygon` | Plain JSON string |

These methods take **no database argument** and are **not applied automatically** by `BaseDbContext`. They add a value converter only, without geometry validation, spatial query translation, SQL type selection, or a structural value comparer.

Use the [geometry storage-path matrix](./point-conversion.md#choose-one-storage-path) before combining conversions and migrations. `[Json]` is a separate property-attribute path with its own serializer options; it is not a synonym for these extensions.
