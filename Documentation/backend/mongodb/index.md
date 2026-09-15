---
title: MongoDB
description: Optional document persistence, BSON conventions, and observation for standalone Arc.
---

Use MongoDB when your Arc application needs document storage. `Cratis.Arc.MongoDB` handles collection injection and BSON conventions while letting you use the MongoDB driver directly. It is optional and does not require Chronicle.

## Start with a read

Follow [getting started](./getting-started.md) to install the package, configure a server/database, and activate your host. Keep that page as the canonical setup path rather than copying an incomplete bootstrap.

## Model your documents

- [Concepts](./concepts.md): store strongly typed values without wrapper documents.
- [Serializers](./serializers.md): understand BSON representations, including date precision and Guid formats.
- [Naming policies](./naming-policies.md): choose collection and member names before writing data.
- [Class mapping](./class-mapping.md): customize individual document types.
- [Convention packs](./convention-packs.md): apply and filter shared conventions.
- [Geospatial types](./geospatial/index.md): store GeoJSON and construct driver query geometry.

## Add live reads and tenancy

- [Observe collections](./observing-collections.md): per-collection change streams, lifetime, and errors.
- [Watch multiple collections](./change-stream-watcher.md): shared database change streams and joined results.
- [Tenancy](./tenancy.md): database naming and scope boundaries.

MongoDB observation is not an event log, and MongoDB persistence does not append Chronicle events. See the separate [Chronicle integration](../chronicle/index.md) if you intentionally add event sourcing.
