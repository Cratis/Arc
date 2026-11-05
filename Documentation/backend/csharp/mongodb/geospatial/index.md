---
title: Cratis Arc geospatial storage and queries
description: Store locations, routes, and boundaries as GeoJSON with Cratis Arc, then build MongoDB spatial filters with driver geometry types.
---

Cratis Arc stores `Cratis.Geospatial` locations, routes, and boundaries as GeoJSON, a document format for geographic shapes. Use these model types for storage and MongoDB driver geometry types for spatial filters. Polygon storage has a [current reader limitation](./polygon.md#current-read-limitation): typed reads fail on the nested coordinate arrays its writer emits.

## Supported types

Choose the shape that matches the question your application needs to answer:

| Type | Store | Query example |
| --- | --- | --- |
| [Point](./point.md) | A single location, such as a store | Find stores near a location |
| [LineString](./linestring.md) | An ordered path, such as a delivery route | Find routes near a location or crossing a region |
| [Polygon](./polygon.md) | An area with an outer boundary and optional holes | Build containment or intersection filters, subject to the reader limitation |

## GeoJSON compatibility

Arc's BSON serializers convert the model types to MongoDB's binary document format using GeoJSON's `type` and `coordinates` fields. Coordinate pairs are always `[longitude, latitude]`. The records and write paths do not validate geometry, so validate coordinates and shapes before storage. The Polygon reader has some ring checks, but they do not provide comprehensive geometry validation.

For filter arguments, use types from `MongoDB.Driver.GeoJsonObjectModel`. A Cratis `Point`, for example, is the stored model value; a driver `GeoJsonPoint<GeoJson2DGeographicCoordinates>` supplies the query location. The individual guides show this distinction alongside spatial index setup. MongoDB's spatial-index and query restrictions still apply.

Nullable geometry needs a storage decision too. The current serializers neither write null geometry values nor read explicit BSON null. Choose an omission policy or a null-aware member serializer as described in [serializer null handling](../serializers.md#error-handling); a nullable annotation alone does not change serialization.

## Getting started

Complete [MongoDB setup](../getting-started.md) to register Arc's serializers and make typed collections available. Then follow the [Point recipe](./point.md) for nearby locations or the [LineString recipe](./linestring.md) for routes. For service areas, read the [Polygon read limitation](./polygon.md#current-read-limitation) before using the boundary and filter examples.
