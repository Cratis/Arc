---
title: LineString serializer
description: Store routes as GeoJSON and query them using driver geometry.
---

Arc's `LineStringSerializer` stores a Cratis line as GeoJSON. Use it for paths, not for computed distance along a route. Complete [MongoDB setup](../getting-started.md) before these model/query fragments.

## Usage

```csharp
using Cratis.Geospatial;
using MongoDB.Bson;

public class DeliveryRoute
{
    public ObjectId Id { get; set; }
    public required LineString Path { get; set; }
}
```

Construction fragment inside a method:

```csharp
var route = new LineString([
    new Point(-122.4194, 37.7749),
    new Point(-122.4185, 37.7750),
    new Point(-122.4170, 37.7755)
]);
```

## Storage format

With camel-case naming, the path member is:

```json
{"path":{"type":"LineString","coordinates":[[-122.4194,37.7749],[-122.4185,37.775],[-122.417,37.7755]]}}
```

Provide at least two points in their intended order. The Cratis record/serializer does not validate that count or geographic ranges. MongoDB may reject invalid geometry when indexing or querying it.

## Querying

For an `IMongoCollection<DeliveryRoute>` named `collection`, this method fragment finds nearby routes:

```csharp
using MongoDB.Driver;
using MongoDB.Driver.GeoJsonObjectModel;

await collection.Indexes.CreateOneAsync(new CreateIndexModel<DeliveryRoute>(
    Builders<DeliveryRoute>.IndexKeys.Geo2DSphere(route => route.Path)));
var origin = new GeoJsonPoint<GeoJson2DGeographicCoordinates>(
    new GeoJson2DGeographicCoordinates(-122.418, 37.775));
var nearby = await collection.Find(Builders<DeliveryRoute>.Filter.Near(
    route => route.Path, origin, maxDistance: 1000)).ToListAsync();
```

The filter argument is a **driver GeoJSON point**, not a Cratis point. Perform index creation during setup. The radius is in meters for this GeoJSON query; it is not distance traveled along the line.

To find routes crossing a region, use `GeoIntersects` with a driver GeoJSON polygon. `GeoWithin` instead asks whether the stored geometry is contained within the query area. See [polygon construction and queries](./polygon.md#querying).

## Limits

Serialization supplies a GeoJSON shape, not validation, a routing engine, or guaranteed query translation from arbitrary geometry methods. Test boundary cases and spatial query results in your target MongoDB version.

`LineString?` expresses CLR optionality only. Arc's current geometry BSON serializers neither serialize a null value nor read explicit BSON null. Design and test an omission policy or a null-aware member serializer before persisting optional geometry; a nullable annotation alone is insufficient. See [serializer null handling](../serializers.md#error-handling).

See [Point](./point.md) and [Polygon](./polygon.md) for the other supported geometry types.
