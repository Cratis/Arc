---
title: Entity Framework Core read-only contexts
description: Configure Cratis Arc query contexts with no-tracking reads and SaveChanges guards, while using database permissions to prevent writes.
---

Use Cratis Arc's read-only Entity Framework Core contexts for code that should query data rather than save changes. Combine the base class with read-only registration to get automatic navigation loading, no-tracking reads, and SaveChanges guards. These guards catch accidental saves; use database permissions when you must prevent writes.

## Define a query context

Derive your query context from `ReadOnlyDbContext`. This declaration assumes your application supplies a `Customer` entity:

```csharp
using Cratis.Arc.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

public class StoreDbContext(DbContextOptions<StoreDbContext> options) : ReadOnlyDbContext(options)
{
    public DbSet<Customer> Customers => Set<Customer>();
}
```

Register it through `WithEntityFrameworkCore` discovery or this explicit service-registration fragment:

```csharp
services.AddReadOnlyDbContextWithConnectionString<StoreDbContext>("Data Source=store.db");
```

Import `Microsoft.Extensions.DependencyInjection` for service registration.

## Inheritance versus registration

| Mechanism | Behavior |
| --- | --- |
| `ReadOnlyDbContext` inheritance | Includes `BaseDbContext` conversions; overrides `SaveChanges()` and `SaveChangesAsync(CancellationToken)` to throw; enables eager loading for ordinary navigations |
| `AddReadOnlyDbContext<T>` | Registers a pooled factory and scoped context; configures `NoTracking` and a SaveChanges interceptor; accepts any EF `DbContext` subclass |
| `AddReadOnlyDbContextWithConnectionString<T>` | Adds provider detection to read-only registration; requires a `BaseDbContext` subclass |

The base class and registration helpers do different work. Inheritance alone does not set `NoTracking` or override the Boolean `acceptAllChangesOnSuccess` SaveChanges overloads. Read-only registration adds an interceptor that rejects the SaveChanges path, including those overloads.

Neither mechanism prohibits raw SQL, `ExecuteUpdate`, `ExecuteDelete`, or writes through another context. Use database credentials with read-only permissions when preventing writes is a requirement.

## Automatic eager loading

`ReadOnlyDbContext` marks the navigation properties returned by EF's `GetNavigations()` as auto-included, so EF loads those related entities without an explicit `Include` call. Inspect the generated queries for your model before relying on this default. The loop does not include EF skip navigations, which represent many-to-many relationships without an explicit join entity.

To disable this behavior globally, add this member to your derived context:

```csharp
protected override bool IsEagerLoadingEnabled => false;
```

To skip auto-includes for an individual query:

```csharp
var customers = await context.Customers.IgnoreAutoIncludes().ToListAsync();
```

To override a particular navigation, configure it after the base call so your setting takes precedence:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);
    modelBuilder.Entity<Customer>().Navigation(customer => customer.Orders).AutoInclude(false);
}
```

These are context/query fragments using your application's entities. EF's own rules for owned navigations still apply.

## Assembly registration

Assembly scanning selects exported, nonabstract `ReadOnlyDbContext` subclasses and excludes `[IgnoreAutoRegistration]` types. It does not discover every arbitrary `DbContext`.

```csharp
services.AddReadModelDbContextsWithConnectionStringFromAssemblies(
    "Data Source=store.db",
    (serviceProvider, options) => options.EnableDetailedErrors(),
    typeof(StoreDbContext).Assembly);
```

Continue with [automatic database hookup](./automatic-database-hookup.md) to understand provider selection and pooled context registration. If clients need refreshed query results after changes, follow [DbSet observation](./observing.md), including its provider setup and lifetime requirements.
