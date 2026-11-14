---
title: Neo4j graph access in Cratis Arc
description: Read Neo4j graphs from Arc C# commands and queries using tenant-aware sessions, typed concept mapping and database naming shared with Chronicle.
---

Cratis Arc's Neo4j integration gives commands and queries a session on the graph database of the current tenant. Add `Cratis.Arc.Neo4j` to inject an `IGraphSession` into model-bound queries, a command's `Provide()` method and ordinary services, with the same convenience the MongoDB integration gives documents. You can use it without Chronicle or event sourcing.

A graph is what events leave behind: Chronicle's Neo4j graph sink writes nodes and relationships from graph projections, and nobody writes to it directly. Arc reads it. That is why the session is a read session by default.

## Start with a read

Follow [getting started](./getting-started.md) to install the package, connect your host to a server and run the first query.

## Read the right graph

- [Tenancy](./tenancy.md): the database naming Arc and Chronicle share, and the single-database mode for development.
- [Connection resolution](./connection-resolution.md): choose a server, database or credentials for each tenant.

## Write the queries

- [Querying](./querying.md): run Cypher through the session and map the rows to your types.
- [Concepts](./concepts.md): bind `ConceptAs<T>` values as parameters and get them back in results.
- [Graph views](./graph-views.md): read Chronicle graph views as typed, nested records through `IGraph`, without writing Cypher.
- [Testing](./testing.md): substitute the session in specifications and command scenarios.

If your documents live in MongoDB instead, see the [MongoDB integration](../mongodb/index.md). Both follow the same [tenancy](../tenancy/index.md) model.
