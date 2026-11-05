---
title: Cratis Arc Point storage and queries
description: Store locations as GeoJSON points with Cratis Arc and find nearby stores using MongoDB driver geometry, a spatial index, and meter-based distances.
---

Cratis Arc stores `Cratis.Geospatial.Point` as GeoJSON so you can keep a location on a document and query for nearby places. Coordinates use longitude first, then latitude.

Complete [MongoDB setup](../getting-started.md) to register the BSON serializers, which convert model values to MongoDB's binary document format. The examples below add a model and query fragments to that configured host.

## Usage

Give each store a required location:

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

Inside a method, construct a point with longitude and latitude. Named arguments use the constructor's capitalized parameter names:

```csharp
var point = new Point(Longitude: -122.4194, Latitude: 37.7749);
```

## Storage format

With camel-case naming configured, Arc writes the location member as:

```json
{"location":{"type":"Point","coordinates":[-122.4194,37.7749]}}
```

The coordinate array is `[longitude, latitude]`. Validate geographic ranges before writing a point because neither the record nor the serializer enforces them.

If a location is optional, `Point?` only makes the .NET property nullable. Arc's current geometry BSON serializers neither write a null value nor read explicit BSON null. Choose and test an omission policy or a null-aware member serializer before persisting optional locations. [Serializer null handling](../serializers.md#error-handling) explains the difference between an absent field and an explicit null.

## Querying

To find stores within 5,000 meters of a location, supply a driver `GeoJsonPoint<GeoJson2DGeographicCoordinates>` as the query origin. The stored property remains a Cratis `Point`; the filter's geometry argument uses the driver's type.

The following method fragment uses an `IMongoCollection<Store>` named `collection`. Create the spatial index during setup, then use the filter when querying:

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

The `2dsphere` index supports spherical geometry queries. With this GeoJSON query point, `maxDistance` is in meters. Keep index creation out of the per-query path.

For stores inside a region, replace the proximity filter with `GeoWithin` and a driver GeoJSON polygon. [Polygon queries](./polygon.md#querying) shows how to construct that argument and distinguishes containment from intersection. Using a driver polygon as a filter argument does not require reading stored Cratis polygons.

Check actual spatial results against your MongoDB deployment. If your documents store a path instead of a location, continue with [LineString storage and queries](./linestring.md).
