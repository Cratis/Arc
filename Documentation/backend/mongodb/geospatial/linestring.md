---
title: LineString Serializer
description: Store routes and paths (ordered point sequences) in MongoDB
---

The LineString serializer stores ordered sequences of connected points as GeoJSON LineString objects, ideal for representing routes, paths, and trajectories.

## Usage

```csharp
using Cratis.Geospatial;

public class DeliveryRoute
{
    public ObjectId Id { get; set; }
    public string RouteName { get; set; }
    public LineString Path { get; set; }
    public DateTime CreatedAt { get; set; }
}

// Create a line string
var points = new[]
{
    new Point(-122.4194, 37.7749),
    new Point(-122.4185, 37.7750),
    new Point(-122.4170, 37.7755)
};

var route = new LineString(points);
```

## Storage Format

LineStrings are serialized as GeoJSON LineString documents:

```json
{
  "_id": ObjectId("..."),
  "routeName": "Downtown Loop",
  "path": {
    "type": "LineString",
    "coordinates": [
      [-122.4194, 37.7749],
      [-122.4185, 37.7750],
      [-122.4170, 37.7755]
    ]
  },
  "createdAt": ISODate("2024-06-09T...")
}
```

## Querying

### Finding Routes That Pass Through a Point

```csharp
// Create a geospatial index
collection.Indexes.CreateOne(
    new CreateIndexModel<DeliveryRoute>(
        Builders<DeliveryRoute>.IndexKeys.Geo2DSphere(r => r.Path)
    )
);

// Find routes near a specific point
var routesNearby = await collection.Find(
    Builders<DeliveryRoute>.Filter.Near(
        r => r.Path,
        new Point(-122.418, 37.775),
        maxDistance: 1000
    )
).ToListAsync();
```

### Finding Routes Intersecting an Area

```csharp
// Find routes within a polygon boundary
var routesInArea = await collection.Find(
    Builders<DeliveryRoute>.Filter.GeoWithin(
        r => r.Path,
        polygon
    )
).ToListAsync();
```

## Common Patterns

### Tracking User Movement

```csharp
public class UserTrack
{
    public ObjectId Id { get; set; }
    public ObjectId UserId { get; set; }
    public LineString Path { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
}

// Record a user's movement over time
var trackPoints = new[]
{
    new Point(-122.419, 37.774),
    new Point(-122.418, 37.775),
    new Point(-122.417, 37.776)
};

var track = new UserTrack
{
    UserId = userId,
    Path = new LineString(trackPoints),
    StartTime = DateTime.UtcNow.AddHours(-1),
    EndTime = DateTime.UtcNow
};
```

## Best Practices

- **Minimum Points**: Ensure at least 2 points in the sequence
- **Coordinate Order**: Use `[longitude, latitude]` for each point
- **Create Indexes**: Add `2dsphere` indexes for spatial query performance
- **Ordered Sequence**: Points should represent movement in chronological or logical order
- **Nullable Support**: Use `LineString?` for optional paths

## Limitations

- MongoDB's spatial queries treat LineStrings as paths but don't enforce properties like "no self-intersection"
- Distance calculations along the path require application-level computation
- Query results with `$near` return distance to the nearest point on the line, not along it

## Related

- [Point](./point.md) — Individual coordinates
- [Polygon](./polygon.md) — Geographic areas
