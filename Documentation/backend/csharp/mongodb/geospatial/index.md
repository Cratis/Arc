---
title: Geospatial Types
description: Store and serialize geographic data with Point, LineString, and Polygon types
---

Arc provides BSON serializers for types from `Cratis.Geospatial`. Their writers produce GeoJSON for MongoDB storage and spatial filters. Before using Polygon, read the [current Polygon reader limitation](./polygon.md#current-read-limitation): typed reads fail on the coordinate nesting its writer emits.

## Supported Types

- **[Point](./point.md)** — Single geographic coordinates (longitude, latitude)
- **[LineString](./linestring.md)** — Routes and paths (ordered sequences of points)
- **[Polygon](./polygon.md)** — Geographic areas with optional interior boundaries (holes)

## GeoJSON Compatibility

Arc's BSON serializers write GeoJSON documents. This does not mean the Cratis model types are accepted as geometry arguments by MongoDB driver's filter builders: use `MongoDB.Driver.GeoJsonObjectModel` types for those arguments, as the individual guides show. The records and write paths do not validate geometry; the Polygon reader contains some ring checks, not comprehensive validation. Server spatial-index restrictions still apply. Optional geometry also needs an explicit [null/omission policy](../serializers.md#error-handling), not just a nullable annotation.

GeoJSON storage enables:

- **MongoDB Spatial Queries** — Use `$near`, `$geoWithin`, and other spatial operators
- **Interoperability** — Exchange GeoJSON with compatible mapping services and GIS tools
- **Standards Compliance** — Follow industry-standard GeoJSON specification

## Getting Started

Choose the type that matches your use case:

- Use **Point** for storing single locations (stores, users, events)
- Use **LineString** for routes, paths, or trajectories
- Use **Polygon** for service areas, regions, or geographic boundaries

See the individual type documentation for implementation examples and best practices.
