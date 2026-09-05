---
title: Geospatial Types
description: Store and serialize geographic data with Point, LineString, and Polygon types
---

Cratis provides first-class support for geospatial data in MongoDB using types from `Cratis.Geospatial`. The serializers follow the GeoJSON standard, making them compatible with MongoDB's geospatial query operators and external mapping services.

## Supported Types

- **[Point](./point.md)** — Single geographic coordinates (longitude, latitude)
- **[LineString](./linestring.md)** — Routes and paths (ordered sequences of points)
- **[Polygon](./polygon.md)** — Geographic areas with optional interior boundaries (holes)

## GeoJSON Compatibility

All geospatial types are serialized in GeoJSON format, enabling:

- **MongoDB Spatial Queries** — Use `$near`, `$geoWithin`, and other spatial operators
- **Interoperability** — Work seamlessly with mapping services and GIS tools
- **Standards Compliance** — Follow industry-standard GeoJSON specification

## Getting Started

Choose the type that matches your use case:

- Use **Point** for storing single locations (stores, users, events)
- Use **LineString** for routes, paths, or trajectories
- Use **Polygon** for service areas, regions, or geographic boundaries

See the individual type documentation for implementation examples and best practices.
