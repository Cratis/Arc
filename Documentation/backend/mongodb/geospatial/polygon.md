---
title: Polygon serializer
description: Store GeoJSON boundaries and distinguish containment from intersection queries.
---

Use a polygon for an area with an exterior ring and optional holes. Arc supplies BSON serialization; your application supplies valid geometry. Complete [MongoDB setup](../getting-started.md) before these model/query fragments.

## Usage

```csharp
using Cratis.Geospatial;
using MongoDB.Bson;

public class ServiceArea
{
    public ObjectId Id { get; set; }
    public required Polygon Boundary { get; set; }
}
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

`Holes` is capitalized in the constructor's named argument. Supply `LinearRing[]` for holes, not a collection of polygons.

## Storage format

With camel-case naming, the boundary member is:

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

The first ring is the shell; later rings are holes. Unlike EF's plain geometry string converters, MongoDB serializers produce GeoJSON numeric coordinate arrays.

## Current read limitation

> [!WARNING]
> The current Arc Polygon BSON reader does not enter the nested coordinate-pair arrays emitted by its writer. Typed materialization of matching documents as `ServiceArea` fails on this GeoJSON shape. The query examples below demonstrate filter construction, **not working Polygon reads**. Use a corrected, tested serializer before relying on those reads.

Writing succeeds, but reading the nested coordinate arrays can throw `InvalidOperationException`: `ReadDouble can only be called when CurrentBsonType is Double, not when CurrentBsonType is Array.` This is a BSON reader limitation, separate from MongoDB's spatial-query support.

To check the serializer's round-trip behavior, run this in a **separate console process** referencing `Cratis.Arc.MongoDB`, without Arc host initialization. The explicit registration is for this isolated probe only; do not repeat it after Arc setup:

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

With the current reader, both iterations print the failure above. A completed read still needs coordinate/topology assertions; this diagnostic does not certify geometry correctness.

## Querying

For an `IMongoCollection<ServiceArea>` named `collection`, create a spatial index during setup and use a **driver GeoJSON point** to find areas intersecting that point:

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

Do not use `GeoWithin(storedPolygon, point)` to invert containment: it asks whether the stored geometry lies within the query geometry, not whether an area contains a point.

For overlapping areas, construct a driver polygon and use `GeoIntersects` (not a nonexistent `Intersects` filter method):

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

For complete containment, substitute `GeoWithin` with that polygon argument. Cratis `Polygon` is the stored model type, not the accepted driver filter geometry type. Test edges, holes, large regions, and boundary points on your target server rather than inferring precise topology semantics from serialization alone.

## Polygon requirements

Validate these before storage: closed rings, at least three distinct vertices plus a closing point, valid coordinate ranges, suitable winding, and no invalid self-intersections. The Cratis records and write path do not validate geometry. The read path has minimum-count and closure checks for rings, not comprehensive geometry validation, and currently encounters the reader failure described above. MongoDB's geospatial validation and index/query restrictions still apply.

Use holes for interior exclusions and separate documents for disconnected regions when appropriate. A list of polygons is not automatically a GeoJSON `MultiPolygon`.

Continue with [Point](./point.md) and [LineString](./linestring.md).
