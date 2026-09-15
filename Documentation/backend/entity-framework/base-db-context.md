---
title: Base DbContext
description: Understand which model conventions BaseDbContext applies and what registration adds.
---

`BaseDbContext` removes repeated model conversion setup. Use it with Arc's context registration helpers so entity-map discovery and application services are also available.

## Define and register a context

Model declarations (supply your application's `Customer` entity):

```csharp
using Cratis.Arc.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

public class StoreDbContext(DbContextOptions<StoreDbContext> options) : BaseDbContext(options)
{
    public DbSet<Customer> Customers => Set<Customer>();
}
```

Registration fragment in an existing host:

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

**Geometry conversion is not automatic.** Configure `AsPoint()`, `AsLineString()`, or `AsPolygon()` explicitly, or choose the separate `[Json]` path. See [point conversion](./point-conversion.md) for the storage distinctions.

[Entity maps](./entity-mapping.md) are applied only when an `IEntityTypeRegistrar` is available through the options' application service provider. Arc registration wires this up. Directly constructing a context, or ordinary EF registration without the corresponding services/options, is not equivalent.

## Pooling and customization

Arc's helpers register a pooled `IDbContextFactory<T>` and a scoped context. Configure options at registration time; do not change them in `OnConfiguring` on a pooled context. Contexts created directly through a factory must be disposed by their caller.

If you override `OnModelCreating`, call `base.OnModelCreating(modelBuilder)` to retain the conventions. Configuration ordering matters: configure explicit overrides after the base call where appropriate.

Pooling does not provide tenant isolation. The helpers capture a connection string rather than resolving one per request. See [registration and provider limits](./getting-started.md#scope-and-provider-limits).
