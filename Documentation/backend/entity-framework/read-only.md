---
title: Read-only DbContext
description: Configure query-oriented contexts while keeping database permissions as the write boundary.
---

For query code, no-tracking reads and accidental-write guards express intent. Arc offers both a base class and registration helpers, but they are **not interchangeable security boundaries**.

## Define a query context

Model declaration fragment; supply the `Customer` entity:

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

Inheritance alone does not override the Boolean `acceptAllChangesOnSuccess` SaveChanges overloads. Registration's interceptor protects the SaveChanges path but does not prohibit raw SQL, `ExecuteUpdate`, `ExecuteDelete`, or writes through another context. Use database credentials with read-only permissions when preventing writes is a requirement.

## Automatic eager loading

`ReadOnlyDbContext` marks navigations returned by EF's `GetNavigations()` as auto-included. Choose purpose-built models and inspect generated queries rather than assuming every relationship is cheap. Skip navigations are not included by that loop.

To disable this behavior globally, add this member to your derived context:

```csharp
protected override bool IsEagerLoadingEnabled => false;
```

To skip auto-includes for an individual query:

```csharp
var customers = await context.Customers.IgnoreAutoIncludes().ToListAsync();
```

To override a particular navigation, configure it **after** the base call:

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

See [automatic database hookup](./automatic-database-hookup.md) for pooling and tenant limitations, then [observation](./observing.md) for live query results.
