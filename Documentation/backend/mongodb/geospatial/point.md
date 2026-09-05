---
title: Point Serializer
description: Store single geographic coordinates (longitude, latitude) in MongoDB
---

The Point serializer stores geographic coordinates as GeoJSON Point objects, compatible with MongoDB's geospatial queries.

## Usage

```csharp
using Cratis.Geospatial;

public class Store
{
    public ObjectId Id { get; set; }
    public string Name { get; set; }
    public Point Location { get; set; }
}

// Create a point
var point = new Point(longitude: -122.4194, latitude: 37.7749);
```

## Storage Format

Points are serialized as GeoJSON Point documents:

```json
{
  "_id": ObjectId("..."),
  "name": "Downtown Store",
  "location": {
    "type": "Point",
    "coordinates": [-122.4194, 37.7749]
  }
}
```

The `coordinates` array follows GeoJSON format: `[longitude, latitude]`.

## Querying

### Finding Nearby Points

Use MongoDB's `$near` operator with geospatial indexes:

```csharp
// Create a geospatial index first
collection.Indexes.CreateOne(
    new CreateIndexModel<Store>(
        Builders<Store>.IndexKeys.Geo2DSphere(s => s.Location)
    )
);

// Query for stores near a point
var nearbyStores = await collection.Find(
    Builders<Store>.Filter.Near(
        s => s.Location,
        new Point(-122.4, 37.78),
        maxDistance: 5000 // 5km in meters
    )
).ToListAsync();
```

### Checking if Point is Within Area

```csharp
// Find points within a polygon boundary
var storesInArea = await collection.Find(
    Builders<Store>.Filter.GeoWithin(
        s => s.Location,
        polygon
    )
).ToListAsync();
```

## Best Practices

- **Coordinate Order**: Always use `[longitude, latitude]` (not latitude, longitude)
- **Create Indexes**: Create `2dsphere` indexes on Point properties for efficient spatial queries
- **Null Handling**: Points can be nullable (`Point?`) for optional locations
- **Validation**: Ensure longitude is between -180 and 180, latitude between -90 and 90

## Related

- [LineString](./linestring.md) — Routes and paths
- [Polygon](./polygon.md) — Geographic areas
