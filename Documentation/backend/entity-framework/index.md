# Entity Framework Core

The Arc provides enhanced support for Entity Framework Core, offering simplified configuration, automatic database setup, and common patterns for working with EF Core in your applications.

## Topics

| Topic | Description |
| ------- | ----------- |
| [Getting Started](./getting-started.md) | How to configure Entity Framework Core with Arc, including auto-discovery and observation support. |
| [Base DbContext](./base-db-context.md) | How to use the base DbContext class provided by the Arc. |
| [Entity Mapping](./entity-mapping.md) | How to configure entities using IEntityMapFor&lt;T&gt; for clean, organized entity configuration. |
| [Read Only DbContexts](./read-only.md) | How to implement read-only database contexts for query scenarios. |
| [Automatic Database hookup](./automatic-database-hookup.md) | How the Arc automatically configures and sets up your databases. |
| [Observing DbSet](./observing.md) | How to monitor entity changes in real-time using reactive extensions. |
| [Common Column Types](./common-column-types.md) | Common column type configurations and conventions. |
| [Property Extensions](./property-extensions.md) | Property configuration extensions for cross-database compatibility. |
| [Json](./json.md) | Working with JSON columns and serialization in Entity Framework Core. |

## Overview

The Entity Framework Core integration in the Arc streamlines database operations by providing sensible defaults, automatic configuration, and patterns that work well with CQRS architecture. Whether you're working with read-write or read-only contexts, the framework handles the complexity of setup and configuration while giving you the flexibility to customize when needed.
