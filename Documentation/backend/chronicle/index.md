# Chronicle

Chronicle is Arc's event sourcing infrastructure. It provides the foundation for building event-sourced applications with full support for aggregates, event stores, and projections.

## Topics

- [Aggregates](aggregates/index.md) - Working with aggregate roots and event sourcing
- [Commands](commands/index.md) - Returning events from commands, event source id resolution, and concurrency scoping
- [Event Source Values Provider](event-source-values-provider.md) - Command context values for event source resolution
- [Read Models](read-models.md) - How read models are hooked up
- [Tenancy](tenancy.md) - Tenant-aware namespaces for event stores and projections
- [Validation](validation.md) - Validation with read models and identity resolution conventions
