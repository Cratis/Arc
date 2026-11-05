---
title: Cratis Arc LineString storage and queries
description: Store ordered routes as GeoJSON with Cratis Arc and find nearby paths using MongoDB driver geometry, without computing travel distance.
---

Cratis Arc stores `Cratis.Geospatial.LineString` as a GeoJSON path so you can query for routes near a location or crossing a region. It preserves an ordered sequence of points; it does not compute distance traveled along a route.

Complete [MongoDB setup](../getting-started.md) to register the BSON serializers, which convert model values to MongoDB's binary document format. The examples below add a route model and query fragments to that configured host.

## Usage

Store the path on your route document:

```csharp
using Cratis.Geospatial;
using MongoDB.Bson;

public class DeliveryRoute
{
    public ObjectId Id { get; set; }
    public required LineString Path { get; set; }
}
```

Inside a method, construct the path in travel order. Each point takes longitude first, then latitude:

```csharp
var route = new LineString([
    new Point(-122.4194, 37.7749),
    new Point(-122.4185, 37.7750),
    new Point(-122.4170, 37.7755)
]);
```

## Storage format

With camel-case naming configured, Arc writes the path member as:

```json
{"path":{"type":"LineString","coordinates":[[-122.4194,37.7749],[-122.4185,37.775],[-122.417,37.7755]]}}
```

Each coordinate pair is `[longitude, latitude]`. Provide at least two points in their intended order and validate geographic ranges before storage. Neither the Cratis record nor the serializer checks the point count or ranges. MongoDB may reject invalid geometry when indexing or querying it.

## Querying

To find routes near a location, query the path against a driver GeoJSON point. This method fragment uses an `IMongoCollection<DeliveryRoute>` named `collection`. Create the spatial index during setup, then use the filter when querying:

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

The stored path is a Cratis `LineString`, while the query origin is a driver `GeoJsonPoint<GeoJson2DGeographicCoordinates>`. For this GeoJSON query, `maxDistance: 1000` sets a 1,000-meter proximity bound. It does not measure distance traveled along the line. Keep index creation out of the per-query path.

For routes crossing a region, use `GeoIntersects` with a driver GeoJSON polygon. Use `GeoWithin` when the stored geometry must be contained within the query area. [Polygon construction and queries](./polygon.md#querying) shows the polygon argument; using it in a filter does not require reading stored Cratis polygons.

## Limits

`LineStringSerializer` supplies the GeoJSON representation. Your application still owns geometry validation and routing logic, and arbitrary geometry methods are not guaranteed to translate into MongoDB queries. Check boundary cases and spatial results against your target MongoDB version.

If a path is optional, `LineString?` only makes the .NET property nullable. Arc's current geometry BSON serializers neither write a null value nor read explicit BSON null. Choose and test an omission policy or a null-aware member serializer before persisting optional paths. [Serializer null handling](../serializers.md#error-handling) explains the difference between an absent field and an explicit null.

Use [Point storage and queries](./point.md) when each document needs one location. For service areas and other boundaries, read the [Polygon guide](./polygon.md), including its typed-read limitation.
