---
title: Use BaseDbContext for EF Core mapping in Arc
description: Use Cratis Arc's BaseDbContext to apply JSON, ConceptAs and Guid conversions, and register the context so entity configurations are discovered.
---

Cratis Arc's `BaseDbContext` applies JSON, `ConceptAs<T>` and Guid conversions so you do not repeat them in every EF Core context. Register the context through Arc's helpers to also apply your `IEntityTypeConfiguration<T>` entity mappings.

## Define and register a context

Inherit from `BaseDbContext`, pass the options to its constructor, and expose your application's `Customer` entity through a `DbSet<Customer>`:

```csharp
using Cratis.Arc.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

public class StoreDbContext(DbContextOptions<StoreDbContext> options) : BaseDbContext(options)
{
    public DbSet<Customer> Customers => Set<Customer>();
}
```

Register the context in your existing host's service collection:

```csharp
services.AddDbContextWithConnectionString<StoreDbContext>(
    "Data Source=store.db",
    (serviceProvider, options) => options.EnableDetailedErrors());
```

Import `Microsoft.Extensions.DependencyInjection` for service registration. The callback is `Action<IServiceProvider, DbContextOptionsBuilder>`.

## Converters and entity maps

During `OnModelCreating`, the base context applies:

- [JSON conversion](./json.md) to properties marked `[Json]`.
- [Concept conversion](./concept-as-conversion.md) to `ConceptAs<T>` properties.
- [Guid conversion](./guid-conversion.md), which adds string conversion for SQLite.

Relevant types include owned entities, types directly exposed through `DbSet<T>`, and entity types referenced directly or through generic properties on those set types. Types stored exclusively inside JSON are excluded from relational concept/Guid mapping.

For geometry properties, configure `AsPoint()`, `AsLineString()`, or `AsPolygon()` explicitly; the base context does not apply those conversions. Choose the separate `[Json]` path if you want JSON storage instead. See [point conversion](./point-conversion.md) to choose a representation.

Arc registration supplies an `IEntityTypeRegistrar` through EF's application service provider. `BaseDbContext` uses it to apply [entity maps](./entity-mapping.md) before the conversions. If maps are missing from the model, check registration first: constructing the context directly or using ordinary EF registration without those services and options does not supply the registrar.

## Pooling and customization

Arc's helpers register a pooled `IDbContextFactory<T>` and a scoped context. Configure options at registration time; do not change them in `OnConfiguring` on a pooled context. Contexts created directly through a factory must be disposed by their caller.

If you override `OnModelCreating`, call `base.OnModelCreating(modelBuilder)` to retain the conventions. Configuration ordering matters: configure explicit overrides after the base call where appropriate.

For tenant-specific databases or identities, configure connection resolution rather than changing pooled options. Arc resolves those values when a connection opens, including on a reused context. See [registration and provider limits](./getting-started.md#scope-and-provider-limits) before choosing a tenant isolation strategy.
