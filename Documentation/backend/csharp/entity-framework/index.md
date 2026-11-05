---
title: Cratis Arc Entity Framework Core integration
description: Use Entity Framework Core with Cratis Arc for relational persistence, query-oriented contexts, JSON conversion, and observed query results.
---

Cratis Arc's Entity Framework Core integration lets your application store and query relational data without requiring Chronicle or event sourcing. Add the optional `Cratis.Arc.EntityFrameworkCore` package when you need relational persistence, then follow [getting started](./getting-started.md) to register your contexts and select a provider. Apply your database schema separately; registration does not create it.

## Topics

| Topic | Description |
| ------- | ----------- |
| [Getting Started](./getting-started.md) | How to configure Entity Framework Core with Arc, including auto-discovery and observation support. |
| [Base DbContext](./base-db-context.md) | Apply Arc's model conventions to your context. |
| [Entity Mapping](./entity-mapping.md) | Keep entity configuration in IEntityTypeConfiguration&lt;T&gt; classes. |
| [Read Only DbContexts](./read-only.md) | How to implement read-only database contexts for query scenarios. |
| [Automatic Database hookup](./automatic-database-hookup.md) | Provider detection, pooled context registration, and connection-string limits. |
| [Upserts](./upserts.md) | Tracked persistence and atomic native upserts for SQL Server, PostgreSQL, and SQLite. |
| [Observing DbSet](./observing.md) | Refresh query results after changes, with provider-specific notification setup and explicit lifetime ownership. |
| [Common Column Types](./common-column-types.md) | Common column type configurations and conventions. |
| [Property Extensions](./property-extensions.md) | Configure value conversion for individual properties. |
| [Json](./json.md) | Store marked properties as JSON with provider-specific column types and configurable serialization. |
| [Geometry storage paths](./point-conversion.md) | Explicit string conversion versus JSON properties and spatial migration declarations. |
| [Guid conversion](./guid-conversion.md) | SQLite string conversion and migration type differences. |
| [Concept conversion](./concept-as-conversion.md) | Store strongly typed values. |

## Overview

Choose a context around the work it performs. Use [BaseDbContext](./base-db-context.md) for Arc's model conventions, or combine [read-only contexts and registration](./read-only.md) for query code with no-tracking reads and SaveChanges guards. Database permissions remain the boundary when writes must be prevented.

For complex properties, choose [JSON conversion](./json.md) or an explicit [geometry storage path](./point-conversion.md) before creating migrations: a JSON string converter is not a native spatial mapping. Once ordinary reads work, [observe a DbSet](./observing.md) if clients need refreshed results. That page explains which writes each provider can detect and how to own the observation's lifetime.
