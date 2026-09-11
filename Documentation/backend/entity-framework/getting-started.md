---
title: Get started with Entity Framework Core
description: Add optional relational persistence to a standalone Arc application.
---

When your Arc application needs relational storage, add `Cratis.Arc.EntityFrameworkCore`. It provides context registration, model conventions, and observation helpers. **Chronicle is not required**: commands can write through EF Core and queries can read the same database without event sourcing.

## Add the integration

Start from a working [ASP.NET Core Arc host](../asp-net-core/index.md) or [lightweight Arc host](../core/getting-started.md), then install:

```bash
dotnet add package Cratis.Arc.EntityFrameworkCore
```

The following is a startup fragment for an existing Arc builder, not a second complete host program. Import `Cratis.Arc` and `Cratis.Arc.EntityFrameworkCore`, plus your host's builder namespace (`Microsoft.AspNetCore.Builder` for ASP.NET Core).

```csharp
builder.AddCratisArc(configureBuilder: arc =>
{
    arc.WithEntityFrameworkCore(options =>
    {
        options.ConnectionString = "Data Source=orders.db";
    });
});
```

This selects SQLite, registers observation services, and discovers public `BaseDbContext` subclasses. It does **not** create your tables or apply migrations. Use your application's migration process before querying.

## Define a context

Give domain values names before using them in the entity. `OrderId` cannot be accidentally substituted for an unrelated integer identifier, and `OrderDescription` keeps the field's intent visible. Define these shared concepts once in the order feature, each in its own file:

```csharp
using Cratis.Concepts;

public record OrderId(int Value) : ConceptAs<int>(Value)
{
    public static readonly OrderId NotSet = new(0);
    public static implicit operator OrderId(int value) => new(value);
}

public record OrderDescription(string Value) : ConceptAs<string>(Value)
{
    public static readonly OrderDescription NotSet = new(string.Empty);
    public static implicit operator OrderDescription(string value) => new(value);
}
```

These model declarations reuse those concepts and demonstrate the required options constructor:

```csharp
using Cratis.Arc.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

public class OrdersDbContext(DbContextOptions<OrdersDbContext> options) : BaseDbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();
}

public class Order
{
    public required OrderId Id { get; set; }
    public required OrderDescription Description { get; set; }
}
```

`BaseDbContext` applies concept value conversion, so the named values still map to primitive columns. Choose and test your application's key-allocation strategy rather than treating `OrderId.NotSet` as a stored identity. See [concept mapping](./concept-as-conversion.md).

With a nonempty connection string and `AutoDiscoverDbContexts = true` (the default), Arc registers discovered contexts through pooled factories and exposes each context as a scoped service. `ReadOnlyDbContext` subclasses receive read-only registration. `[IgnoreAutoRegistration]` excludes a context. With an empty connection string, discovery returns without registering contexts.

A registration checkpoint is that a service scope can resolve `OrdersDbContext` and `IDbContextFactory<OrdersDbContext>`. Successful registration alone does not prove the database is reachable or its schema is ready.

## Customize registration

For a context that needs a different connection or options, disable discovery and register it explicitly. This is an alternative startup fragment:

```csharp
builder.AddCratisArc(configureBuilder: arc =>
{
    arc.WithEntityFrameworkCore(
        configureOptions: options =>
        {
            options.ConnectionString = "Data Source=orders.db";
            options.AutoDiscoverDbContexts = false;
        },
        configureEfCore: ef =>
        {
            ef.AddDbContext<OrdersDbContext>((serviceProvider, options) =>
            {
                options.EnableDetailedErrors();
            });
        });
});
```

The callback takes **two arguments**, `IServiceProvider` and `DbContextOptionsBuilder`. Avoid sensitive-data logging in production.

| Option | Default | Purpose |
| --- | --- | --- |
| `ConnectionString` | `""` | Shared connection for discovery and builder registration without an explicit connection |
| `AutoDiscoverDbContexts` | `true` | Discover eligible contexts when the connection string is nonempty |
| `JsonConverters` | Empty list | Append converters to EF's JSON conversion options; see [JSON conversion](./json.md#registering-custom-converters) |

## Direct service registration

You can also use the registration helpers on `IServiceCollection`. This fragment assumes `services` belongs to your existing host:

```csharp
services.AddEntityFrameworkCoreObservation();
services.AddDbContextWithConnectionString<OrdersDbContext>("Data Source=orders.db");
```

Import `Cratis.Arc.EntityFrameworkCore.Observe` and `Microsoft.Extensions.DependencyInjection` as well as the context namespaces above. The helper adds observation interceptors when observation services are available at options creation. Register everything before building the service provider.

Direct EF registration is not a replacement for Arc host activation. In particular, `Observe()` uses Arc's initialized query context and service provider. Use the normal Arc bootstrap if you use Arc observation.

## Scope and provider limits

The built-in connection detection supports SQLite, PostgreSQL, and SQL Server. See [automatic database hookup](./automatic-database-hookup.md) for connection patterns and assembly scanning.

Pooled factories capture the configured connection string. They **do not automatically select a database per Arc tenant**. Tenant isolation requires an application-designed, tested strategy; do not capture request-scoped tenant state in pooled options or assume MongoDB database naming applies to EF.

Next, configure [entity mappings](./entity-mapping.md), choose [read-only behavior](./read-only.md), or add [observation](./observing.md) after verifying ordinary database reads and writes.
