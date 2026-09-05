---
title: Polygon Serializer
description: Store geographic areas with optional interior boundaries (holes) in MongoDB
---

The Polygon serializer stores geographic boundaries as GeoJSON Polygon objects, supporting complex shapes with interior holes for representing service areas, regions, and geographic boundaries.

## Usage

```csharp
using Cratis.Geospatial;

public class ServiceArea
{
    public ObjectId Id { get; set; }
    public string AreaName { get; set; }
    public Polygon Boundary { get; set; }
}

// Create a simple polygon
var shellPoints = new[]
{
    new Point(-122.4194, 37.7749),
    new Point(-122.4170, 37.7755),
    new Point(-122.4160, 37.7740),
    new Point(-122.4194, 37.7749) // Must close the ring
};

var simpleArea = new ServiceArea
{
    AreaName = "Downtown",
    Boundary = new Polygon(new LinearRing(shellPoints), holes: [])
};

// Create a polygon with a hole (excluded zone)
var holePoints = new[]
{
    new Point(-122.4180, 37.7750),
    new Point(-122.4175, 37.7752),
    new Point(-122.4170, 37.7748),
    new Point(-122.4180, 37.7750) // Must close the ring
};

var areaWithHole = new ServiceArea
{
    AreaName = "Downtown (with exclusion)",
    Boundary = new Polygon(
        new LinearRing(shellPoints),
        holes: [new LinearRing(holePoints)]
    )
};
```

## Storage Format

Polygons are serialized as GeoJSON Polygon documents:

```json
{
  "_id": ObjectId("..."),
  "areaName": "Downtown",
  "boundary": {
    "type": "Polygon",
    "coordinates": [
      [
        [-122.4194, 37.7749],
        [-122.4170, 37.7755],
        [-122.4160, 37.7740],
        [-122.4194, 37.7749]
      ]
    ]
  }
}
```

With a hole:

```json
{
  "boundary": {
    "type": "Polygon",
    "coordinates": [
      [
        [-122.4194, 37.7749],
        [-122.4170, 37.7755],
        [-122.4160, 37.7740],
        [-122.4194, 37.7749]
      ],
      [
        [-122.4180, 37.7750],
        [-122.4175, 37.7752],
        [-122.4170, 37.7748],
        [-122.4180, 37.7750]
      ]
    ]
  }
}
```

The first coordinate array is the exterior shell; subsequent arrays are interior holes.

## Querying

### Finding Points Within Area

```csharp
// Create a geospatial index
collection.Indexes.CreateOne(
    new CreateIndexModel<ServiceArea>(
        Builders<ServiceArea>.IndexKeys.Geo2DSphere(a => a.Boundary)
    )
);

// Find areas containing a point
var areasContainingPoint = await collection.Find(
    Builders<ServiceArea>.Filter.GeoWithin(
        a => a.Boundary,
        point
    )
).ToListAsync();
```

### Finding Overlapping Polygons

```csharp
// Find areas that intersect with a given polygon
var overlappingAreas = await collection.Find(
    Builders<ServiceArea>.Filter.Intersects(
        a => a.Boundary,
        queryPolygon
    )
).ToListAsync();
```

## Common Patterns

### Service Territory Definition

```csharp
public class ServiceTerritory
{
    public ObjectId Id { get; set; }
    public ObjectId CompanyId { get; set; }
    public string TerritoryName { get; set; }
    public Polygon ServingArea { get; set; }
    public List<Polygon> ExcludedZones { get; set; } = [];
}

// Single territory with excluded zones represented in the polygon
var territory = new ServiceTerritory
{
    CompanyId = companyId,
    TerritoryName = "Northern District",
    ServingArea = new Polygon(shellRing, holes: excludedZones)
};
```

### Multi-Part Regions (Using Multiple Documents)

```csharp
// For disconnected regions, store as separate documents
// or use MultiPolygon pattern (multiple Polygons in a collection)
var islands = new[]
{
    new ServiceArea { AreaName = "Island A", Boundary = polygonA },
    new ServiceArea { AreaName = "Island B", Boundary = polygonB }
};
```

## Polygon Requirements

When creating Polygon instances, ensure:

- **Closed Rings**: First and last points of each ring must be identical
- **Minimum Points**: Each ring must have at least 4 points (3 unique + 1 closing duplicate)
- **Winding Order**: Exterior ring vertices should follow right-hand rule (counter-clockwise when viewed from above)
- **No Self-Intersection**: Rings should not cross themselves
- **Hole Ordering**: Hole rings should follow clockwise winding (interior winding)

## Best Practices

- **Create Indexes**: Add `2dsphere` indexes on Polygon properties for spatial query performance
- **Use Holes for Exclusions**: Instead of multiple polygons, use holes for interior exclusions
- **Validate Geometry**: Ensure polygons are valid GeoJSON before storing
- **Nullable Support**: Use `Polygon?` for optional boundaries
- **Query Strategically**: Use `$geoWithin` to find points/areas within a region

## Performance Considerations

- **Geospatial Indexes**: Essential for good query performance on large datasets
- **Polygon Complexity**: Very complex polygons (many points) may slow queries
- **Batch Operations**: Group spatial queries when checking multiple points

## Related

- [Point](./point.md) — Individual coordinates
- [LineString](./linestring.md) — Routes and paths
