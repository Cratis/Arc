---
title: Point serializer
description: Store GeoJSON points and use MongoDB driver geometry in query filters.
---

Arc stores `Cratis.Geospatial.Point` as GeoJSON. Complete [MongoDB setup](../getting-started.md) first; the snippets below are model declarations and query-method fragments, not a standalone host.

## Usage

```csharp
using Cratis.Geospatial;
using MongoDB.Bson;

public class Store
{
    public ObjectId Id { get; set; }
    public required string Name { get; set; }
    public required Point Location { get; set; }
}
```

Inside a method, construct a point with positional arguments or the correctly capitalized names:

```csharp
var point = new Point(Longitude: -122.4194, Latitude: 37.7749);
```

## Storage format

With camel-case naming, the location member is:

```json
{"location":{"type":"Point","coordinates":[-122.4194,37.7749]}}
```

Coordinates are `[longitude, latitude]`. Validate ranges in your application; the record/serializer does not enforce geographic validity.

`Point?` expresses CLR optionality only. Arc's current geometry BSON serializers neither serialize a null value nor read explicit BSON null. Design and test an omission policy or a null-aware member serializer before persisting optional geometry; a nullable annotation alone is insufficient. See [serializer null handling](../serializers.md#error-handling).

## Querying

The driver's geometry arguments are **`GeoJsonPoint<GeoJson2DGeographicCoordinates>`**, not Cratis `Point`. The following fragment uses an `IMongoCollection<Store>` named `collection`:

```csharp
using MongoDB.Driver;
using MongoDB.Driver.GeoJsonObjectModel;

await collection.Indexes.CreateOneAsync(new CreateIndexModel<Store>(
    Builders<Store>.IndexKeys.Geo2DSphere(store => store.Location)));

var origin = new GeoJsonPoint<GeoJson2DGeographicCoordinates>(
    new GeoJson2DGeographicCoordinates(-122.4, 37.78));
var nearbyStores = await collection.Find(Builders<Store>.Filter.Near(
    store => store.Location,
    origin,
    maxDistance: 5000)).ToListAsync();
```

This uses a `2dsphere` index and a GeoJSON query point, so the distance bound is in meters. Index creation is normally deployment/setup work, not something to repeat on every query.

To find stored points within a region, use `GeoWithin` with a **driver GeoJSON polygon**. See [polygon queries](./polygon.md#querying) for construction and the distinction between containment and intersection. These examples establish API shape; test actual spatial results against your MongoDB deployment.

Continue with [LineString](./linestring.md) for paths or [Polygon](./polygon.md) for areas.
