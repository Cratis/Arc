---
title: MongoDB persistence in Cratis Arc
description: Use MongoDB document storage in Cratis Arc with injectable collections, BSON mapping, tenant-aware connections, and shared change-stream observation.
---

Cratis Arc's MongoDB integration gives commands and queries access to document storage through the MongoDB driver. Add `Cratis.Arc.MongoDB` to inject typed collections and configure how your .NET types are stored as BSON, MongoDB's binary document format. You can use it without Chronicle or event sourcing.

## Start with a read

Follow [getting started](./getting-started.md) to install the package, connect your host to a database, and resolve an `IMongoCollection<T>`. Get ordinary reads working before adding live queries: those use change streams and need a replica set or sharded cluster with permission to watch the database.

## Model your documents

- [Concepts](./concepts.md): store strongly typed values without wrapper documents.
- [Serializers](./serializers.md): understand BSON representations, including date precision and Guid formats.
- [Naming policies](./naming-policies.md): choose collection and member names before writing data.
- [Class mapping](./class-mapping.md): customize individual document types.
- [Convention packs](./convention-packs.md): apply and filter shared conventions.
- [Geospatial types](./geospatial/index.md): store GeoJSON and construct driver query geometry.

## Add live reads and tenancy

- [Observe collections](./observing-collections.md): observations over the shared change stream, lifetime, and errors.
- [Watch multiple collections](./change-stream-watcher.md): shared database change streams and joined results.
- [Tenancy](./tenancy.md): database naming and scope boundaries.
- [Connection resolution](./connection-resolution.md): choose a server, database, or credentials for each tenant.

Graph data lives in Neo4j rather than MongoDB; the [Neo4j integration](../neo4j/index.md) gives the same tenant-aware convenience for a property graph.

Observation refreshes query results when stored documents change; it is not an event log. Storing a document does not append Chronicle events. If you need event sourcing as well, continue with the separate [Chronicle integration](../chronicle/index.md).
