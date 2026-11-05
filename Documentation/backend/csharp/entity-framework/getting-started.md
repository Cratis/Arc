---
title: Set up Entity Framework Core in Cratis Arc
description: Add relational storage to a Cratis Arc host, define an EF Core context with named domain values, and choose automatic or explicit registration.
---

Add `Cratis.Arc.EntityFrameworkCore` when your Cratis Arc commands and queries need relational storage. This setup registers an orders context with SQLite and maps named domain values to database columns. You can use EF Core without Chronicle or event sourcing; commands write through EF Core and queries read the same database.

## Add the integration

Start from a working [ASP.NET Core Arc host](../asp-net-core/index.md) or [lightweight Arc host](../core/getting-started.md), then install:

```bash
dotnet add package Cratis.Arc.EntityFrameworkCore
```

In your existing host's Arc configuration, call `WithEntityFrameworkCore` and set the connection string. Import `Cratis.Arc` and `Cratis.Arc.EntityFrameworkCore`, plus your host's builder namespace (`Microsoft.AspNetCore.Builder` for ASP.NET Core):

```csharp
builder.AddCratisArc(configureBuilder: arc =>
{
    arc.WithEntityFrameworkCore(options =>
    {
        options.ConnectionString = "Data Source=orders.db";
    });
});
```

The `Data Source=orders.db` connection string selects SQLite. Arc registers observation services and discovers public `BaseDbContext` subclasses, including the context you will define next. It does not create tables or apply migrations; use your application's migration process before querying.

## Define a context

Define the order's identifier and description as `ConceptAs<T>` records: named wrappers around an integer and a string. Keep these shared concepts in the order feature, each in its own file:

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

Define the order entity with those concepts, then expose it through `OrdersDbContext`. The context must pass its options to the base constructor:

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

`BaseDbContext` converts `OrderId` to an integer column and `OrderDescription` to a string column. The entity keeps its named C# types. Choose how the application allocates order identifiers; `OrderId.NotSet` is a placeholder, not a stored identity strategy. See [concept mapping](./concept-as-conversion.md) for conversion details.

With a nonempty connection string and `AutoDiscoverDbContexts = true` (the default), Arc registers discovered contexts through pooled factories and exposes each context as a scoped service. `ReadOnlyDbContext` subclasses receive read-only registration. `[IgnoreAutoRegistration]` excludes a context. With an empty connection string, discovery returns without registering contexts.

Check registration by resolving `OrdersDbContext` and `IDbContextFactory<OrdersDbContext>` from a service scope. Then apply your schema and check an ordinary database read and write. Resolving the services confirms registration, not database access or schema readiness.

## Customize registration

Use explicit registration when a context needs its own options. Replace the earlier startup configuration with the following to disable discovery and enable detailed EF errors for `OrdersDbContext`. To use a different connection for a context, pass a connection string to the `AddDbContext<TContext>` overload.

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

The callback receives `IServiceProvider` and `DbContextOptionsBuilder`. Keep these options independent of request-scoped state because pooled contexts reuse them. Avoid sensitive-data logging in production.

| Option | Default | Purpose |
| --- | --- | --- |
| `ConnectionString` | `""` | Shared connection for discovery and builder registration without an explicit connection |
| `AutoDiscoverDbContexts` | `true` | Discover eligible contexts when the connection string is nonempty |
| `JsonConverters` | Empty list | Append converters to EF's JSON conversion options; see [JSON conversion](./json.md#registering-custom-converters) |
| `ConnectionResolution` | Nothing resolved | Resolve the connection per tenant; see [connection resolution](./connection-resolution.md) |

`ConnectionString`, `AutoDiscoverDbContexts` and `ConnectionResolution` also bind from the `Cratis:EntityFrameworkCore` configuration section; `configureOptions` runs after binding and wins.

## Direct service registration

If you manage registration through your host's `IServiceCollection`, add observation services before the context helper:

```csharp
services.AddEntityFrameworkCoreObservation();
services.AddDbContextWithConnectionString<OrdersDbContext>("Data Source=orders.db");
```

Import `Cratis.Arc.EntityFrameworkCore.Observe` and `Microsoft.Extensions.DependencyInjection` as well as the context namespaces above. The helper adds observation interceptors when observation services are available at options creation. Register everything before building the service provider.

Keep the normal Arc host bootstrap when using these helpers. In particular, `Observe()` needs Arc's initialized query context and service provider; registering EF services alone does not initialize them.

## Scope and provider limits

The built-in connection detection supports SQLite, PostgreSQL, and SQL Server. See [automatic database hookup](./automatic-database-hookup.md) for connection patterns and assembly scanning.

Without [connection resolution](./connection-resolution.md), pooled contexts use the configured connection string for every tenant. Configure resolution to select a tenant's database or identity when a connection opens, including on a reused context. Do not capture request-scoped tenant state in pooled options or assume MongoDB database naming applies to EF. Check tenant isolation across pooled reuse as part of your application's storage tests.

Next, configure [entity mappings](./entity-mapping.md), choose [read-only behavior](./read-only.md), or add [observation](./observing.md) after verifying ordinary database reads and writes.
