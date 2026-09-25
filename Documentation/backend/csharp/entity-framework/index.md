---
title: Entity Framework Core
description: Optional relational persistence for standalone Arc applications.
---

Add relational persistence when your Arc application needs it. The optional `Cratis.Arc.EntityFrameworkCore` package provides context registration, model conversion, and observation without requiring Chronicle or event sourcing. Begin with [getting started](./getting-started.md); registration selects a provider but does not create your schema.

## Topics

| Topic | Description |
| ------- | ----------- |
| [Getting Started](./getting-started.md) | How to configure Entity Framework Core with Arc, including auto-discovery and observation support. |
| [Base DbContext](./base-db-context.md) | How to use the base DbContext class provided by the Arc. |
| [Entity Mapping](./entity-mapping.md) | How to configure entities using IEntityTypeConfiguration&lt;T&gt; for clean, organized entity configuration. |
| [Read Only DbContexts](./read-only.md) | How to implement read-only database contexts for query scenarios. |
| [Automatic Database hookup](./automatic-database-hookup.md) | Provider detection, pooled context registration, and connection-string limits. |
| [Observing DbSet](./observing.md) | How to monitor entity changes in real-time using reactive extensions. |
| [Common Column Types](./common-column-types.md) | Common column type configurations and conventions. |
| [Property Extensions](./property-extensions.md) | Property configuration extensions for cross-database compatibility. |
| [Json](./json.md) | Working with JSON columns and serialization in Entity Framework Core. |
| [Geometry storage paths](./point-conversion.md) | Explicit string conversion versus JSON properties and spatial migration declarations. |
| [Guid conversion](./guid-conversion.md) | SQLite string conversion and migration type differences. |
| [Concept conversion](./concept-as-conversion.md) | Store strongly typed values. |

## Overview

The Entity Framework Core integration in the Arc streamlines database operations by providing sensible defaults, automatic configuration, and patterns that work well with CQRS architecture. Whether you're working with read-write or read-only contexts, the framework handles the complexity of setup and configuration while giving you the flexibility to customize when needed.
