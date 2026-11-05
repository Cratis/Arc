---
title: Cratis Arc Polygon storage and query limits
description: Cratis Arc writes Polygon boundaries as GeoJSON, but typed reads currently fail. Build MongoDB spatial filters with driver geometry types.
---

Cratis Arc writes `Cratis.Geospatial.Polygon` as a GeoJSON area with an outer boundary and optional holes. The current reader cannot read the nested coordinates its writer emits, so typed reads fail. Read the [current read limitation](#current-read-limitation) before relying on polygon query results.

The examples below show a boundary model, its storage shape, and MongoDB filter construction. They assume [MongoDB setup](../getting-started.md), which registers the BSON serializers that convert model values to MongoDB's binary document format. Your application must supply valid geometry.

## Usage

Declare the boundary on your service-area document:

```csharp
using Cratis.Geospatial;
using MongoDB.Bson;

public class ServiceArea
{
    public ObjectId Id { get; set; }
    public required Polygon Boundary { get; set; }
}
```

Inside a method, construct a closed outer ring by repeating the first point at the end. Each point takes longitude first, then latitude:

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

The outer ring is the polygon's shell. Pass inner rings through the capitalized `Holes` argument as a `LinearRing[]`; these rings exclude areas inside the shell. Use an empty array when there are no holes.

## Storage format

With camel-case naming configured, Arc writes the boundary member as:

```json
{
    "boundary": {
        "type": "Polygon",
        "coordinates": [
            [
                [0, 0],
                [1, 0],
                [0, 1],
                [0, 0]
            ]
        ]
    }
}
```

The first ring is the shell; later rings are holes. Each coordinate pair is `[longitude, latitude]`. MongoDB serializers write these as GeoJSON numeric arrays, unlike EF's plain geometry string converters.

## Current read limitation

:::caution[Typed Polygon reads fail]
The current Arc Polygon BSON reader does not enter the nested coordinate-pair arrays emitted by its writer. When MongoDB returns a matching document, converting it into a `ServiceArea` fails on this shape. The query examples below show filter construction, not working Polygon reads. Use a corrected, tested serializer before relying on those reads.
:::

The writer emits the nested arrays, but the reader calls `ReadDouble` before entering each coordinate pair. Reading can therefore throw `InvalidOperationException`: `ReadDouble can only be called when CurrentBsonType is Double, not when CurrentBsonType is Array.` This BSON reader limitation is separate from MongoDB's spatial-query support.

To check whether a serializer can read back what it writes, use the following diagnostic in a separate console process referencing `Cratis.Arc.MongoDB`, without Arc host initialization. Registering `PolygonSerializer` explicitly is only for this isolated diagnostic. Do not repeat that registration after Arc setup:

```csharp
using Cratis.Arc.MongoDB;
using Cratis.Geospatial;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

BsonSerializer.RegisterSerializer(new PolygonSerializer());
var triangle = new LinearRing([
    new Point(0, 0), new Point(1, 0),
    new Point(0, 1), new Point(0, 0)
]);
var hole = new LinearRing([
    new Point(0.1, 0.1), new Point(0.1, 0.2),
    new Point(0.2, 0.1), new Point(0.1, 0.1)
]);
Polygon[] polygons = [new(triangle, Holes: []), new(triangle, Holes: [hole])];
foreach (var polygon in polygons)
{
    var bson = polygon.ToBson();
    try
    {
        var restored = BsonSerializer.Deserialize<Polygon>(bson);
        Console.WriteLine($"Read completed: {restored.Holes.Length} holes; verify coordinates too.");
    }
    catch (InvalidOperationException error)
    {
        Console.WriteLine($"Read failed: {error.Message}");
    }
}
```

With the current reader, both iterations reach the failure above. If you supply a corrected serializer and the reads complete, also assert the restored coordinates and ring relationships. Completing this diagnostic alone does not establish geometry correctness.

## Querying

To construct a filter for service areas intersecting a location, supply a driver GeoJSON point to `GeoIntersects`. This method fragment assumes an `IMongoCollection<ServiceArea>` named `collection` and creates the spatial index during setup. Its `ToListAsync()` call still encounters the reader limitation when matching polygons are returned:

```csharp
using MongoDB.Driver;
using MongoDB.Driver.GeoJsonObjectModel;

await collection.Indexes.CreateOneAsync(new CreateIndexModel<ServiceArea>(
    Builders<ServiceArea>.IndexKeys.Geo2DSphere(area => area.Boundary)));
var point = new GeoJsonPoint<GeoJson2DGeographicCoordinates>(
    new GeoJson2DGeographicCoordinates(0.1, 0.1));
var containingAreas = await collection.Find(Builders<ServiceArea>.Filter.GeoIntersects(
    area => area.Boundary, point)).ToListAsync();
```

`GeoWithin` asks whether the stored geometry lies within the query geometry. Calling `GeoWithin(storedPolygon, point)` does not reverse that relationship to find an area containing a point.

For overlapping areas, construct a driver polygon and pass it to `GeoIntersects`. The filter method is named `GeoIntersects`, not `Intersects`:

```csharp
var queryPolygon = new GeoJsonPolygon<GeoJson2DGeographicCoordinates>(
    new GeoJsonPolygonCoordinates<GeoJson2DGeographicCoordinates>(
        new GeoJsonLinearRingCoordinates<GeoJson2DGeographicCoordinates>([
            new GeoJson2DGeographicCoordinates(0, 0),
            new GeoJson2DGeographicCoordinates(1, 0),
            new GeoJson2DGeographicCoordinates(0, 1),
            new GeoJson2DGeographicCoordinates(0, 0)
        ])));
var overlapping = await collection.Find(Builders<ServiceArea>.Filter.GeoIntersects(
    area => area.Boundary, queryPolygon)).ToListAsync();
```

For complete containment within the query polygon, use `GeoWithin` with the same polygon argument. Cratis `Polygon` remains the stored model type; the driver geometry type supplies the filter argument. Check edges, holes, large regions, and boundary points on your target server because serialization alone does not establish spatial-query semantics.

## Polygon requirements

Before storage, validate that each ring is closed, has at least three distinct vertices plus a closing point, and uses valid coordinate ranges. Check winding and self-intersections too. The Cratis records and write path do not validate geometry. The reader has minimum-count and closure checks for rings, but those are not comprehensive validation and it currently fails on the nested arrays described above. MongoDB's geospatial validation and index/query restrictions still apply.

If a boundary is optional, a nullable annotation does not make its serializer null-aware. The current geometry serializers neither write null values nor read explicit BSON null. Choose and test an omission policy or a null-aware member serializer as described in [serializer null handling](../serializers.md#error-handling).

Use holes for interior exclusions and separate documents for disconnected regions when appropriate. A list of polygons is not automatically a GeoJSON `MultiPolygon`.

If your next query is about a single location or a route, use the [Point recipe](./point.md) or [LineString recipe](./linestring.md) instead.
